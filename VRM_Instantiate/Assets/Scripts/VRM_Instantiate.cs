using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UniGLTF;

namespace UniVRM10.Instantiate
{
    public class VRM_Instantiate : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public String filter = null;
            public String customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public String file = null;
            public int maxFile = 0;
            public String fileTitle = null;
            public int maxFileTitle = 0;
            public String initialDir = null;
            public String title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public String defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public String templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }
        static string Filter(params string[] filters)
        {
            return string.Join("\0", filters) + "\0";
        }

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        private Vrm10Instance _loadedVrm;

        void Start()
        {

        }

        public async void OnClickVRMInstantiate()
        {
            /* FilePathを取得 */
#if UNITY_STANDALONE_WIN
            var path = FileDialog("open VRM", "vrm");
            Debug.Log(path);
#elif UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanel("Open VRM", "", "vrm");
#else
            var path = Application.dataPath + "/default.vrm";
#endif

            /* Pathのチェック */
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            var ext = Path.GetExtension(path).ToLower();
            if (ext != ".vrm")
            {
                Debug.LogWarning($"{path} is not vrm");
                return;
            }

            /* 生成 */
            try
            {
                _loadedVrm = await Vrm10.LoadPathAsync(path,
                    canLoadVrm0X: true,
                    showMeshes: false,
                    materialGenerator: new UrpVrm10MaterialDescriptorGenerator());
                if (_loadedVrm == null)
                {
                    return;
                }

                var instance = _loadedVrm.GetComponent<RuntimeGltfInstance>();
                instance.ShowMeshes();
                instance.EnableUpdateWhenOffscreen();
                instance.transform.Rotate(0.0f,180.0f,0.0f);
            }
            catch (OperationCanceledException)
            {
                // Do nothing
            }
        }

        public static string FileDialog(string title, params string[] extensions)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);

            var filters = new List<string>();
            filters.Add("All Files"); filters.Add("*.*");
            foreach (var ext in extensions)
            {
                filters.Add(ext); filters.Add("*" + ext);
            }
            ofn.filter = Filter(filters.ToArray());
            ofn.filterIndex = 2;
            ofn.file = new string(new char[256]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = UnityEngine.Application.dataPath;
            ofn.title = title;
            //ofn.defExt = "PNG";
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;//OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR
            if (!GetOpenFileName(ofn))
            {
                return null;
            }

            return ofn.file;
        }

    }
}
