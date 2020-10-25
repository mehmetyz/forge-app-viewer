using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ForgeApp.Controllers;
namespace ForgeApp.Data
{
    public class ForgeControl
    {
        //State text of all progress.
        public string Status { get; set; }
        
        //Check if authorized.
        public bool IsAuthorized { get; set; }

        //Check if ViewerPage is load for NavBar buttons.
        public bool IsViewerPageLoad { get; set; }

        //check all object are loaded 
        public bool IsObjectsLoad { get; set; }

        //check if any dialog is showed.
        public bool DialogShowed { get; set; }


        //anything is changed, throw this event.
        public event EventHandler Changed;

        //New oss data manager.
        public OSSRequest OSS { get; set; }

        public ForgeControl()
        {

        }

        //throw the changed event.
        public void SetChanged()
        {
            Changed?.Invoke(null,new EventArgs());
        }

        //Get all buckets.
        public async Task<List<OSSRequest.Bucket>> GetAllBuckets()
        {
            return await OSS?.GetAllBuckets();
        }

        //Get all object with related to bucket key.
        public async Task<List<OSSRequest.BucketObject>> GetAllObjects(string bucketKey)
        {
            return await OSS?.GetAllObjects(bucketKey);
        }

        /// <summary>
        /// Upload the file into bucket (Autodesk Forge)
        /// </summary>
        /// <param name="bucketKey">Bucket key where the file will be uploaded</param>
        /// <param name="path">File path</param>
        /// <param name="length">File size</param>
        /// <returns></returns>
        public async Task UploadObject(string bucketKey,string path,long length)
        {

            if (length >= 5 * 1024 * 1024)
                await this.OSS.UploadChunkObject(bucketKey, new Controllers.OSSRequest.OSSFile(path));
            else
                await this.OSS.UploadObject(bucketKey, new Controllers.OSSRequest.OSSFile(path));
        }

    }
}
