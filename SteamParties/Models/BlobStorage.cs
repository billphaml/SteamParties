using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SteamParties.Models
{
	public class BlobStorage
	{
		// The key string for the Azure Blobs data.
		const string BLOB_KEY = "";
		// The name for the container labeled 'users'
		const string BLOB_USERS_NAME = "users";

		/// <summary>
		/// Create the passed in container name if it does not exist.
		/// </summary>
		/// <param name="name">The name of the container you want to create.</param>
		public static async void CreateContainerNotExist(string name)
		{
            BlobServiceClient serviceClient = new BlobServiceClient(BLOB_KEY);
            try
            {
                await serviceClient.CreateBlobContainerAsync(BLOB_USERS_NAME);
            }
            catch (Exception) { }
        }

		/// <summary>
		/// Creates the user container if it doesn't exist.
		/// </summary>
		public static void CreateContainerNotExist()
		{
			CreateContainerNotExist(BLOB_USERS_NAME);
		}

		/// <summary>
		/// Get a List of IDs, a list of users, that exist as blobs.
		/// </summary>
		/// <returns>Returns a list of strings that contains the blobs google ids.</returns>
		public static async Task<List<string>> GetIDS()
		{
			CreateContainerNotExist();
			// Make clients
			BlobServiceClient serviceClient = new BlobServiceClient(BLOB_KEY);
			BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(BLOB_USERS_NAME);
			AsyncPageable<BlobItem> returned = containerClient.GetBlobsAsync();
			List<BlobItem> pages = (List<BlobItem>) returned.AsPages();
			List<string> ids = new List<string>();
			foreach(BlobItem item in pages)
				ids.Add(item.Name);
			return ids;
		}

		/// <summary>
		/// Clear out the blob of the specified user.
		/// </summary>
		/// <param name="googleID">The name of the blob text file.</param>
		/// <returns>Returns nothing.</returns>
		public static async Task ClearUserBlobAsync(string googleID)
		{
			BlobServiceClient serviceClient = new BlobServiceClient(BLOB_KEY);
			BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(BLOB_USERS_NAME);
			await containerClient.DeleteBlobAsync(googleID + ".txt");
		}

		/// <summary>
		/// Get the contents of a blob text file.
		/// </summary>
		/// <param name="googleID">Name of the blob text file.</param>
		/// <returns>Returns a string of the contents of the file.</returns>
		public static async Task<string> GetUserData(string googleID)
		{
			CreateContainerNotExist();
			// Make clients
			BlobServiceClient serviceClient = new BlobServiceClient(BLOB_KEY);
			BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(BLOB_USERS_NAME);
			string file = googleID + ".txt";
			BlobClient blobClient = containerClient.GetBlobClient(file);
			// Download blob
			BlobDownloadInfo download = await blobClient.DownloadAsync();
			Stream s = download.Content;
			StreamReader reader = new StreamReader(s);
			return reader.ReadToEnd();
		}

		/// <summary>
		/// Set the contents of a blob file, with the name of googleID.
		/// </summary>
		/// <param name="googleID">Name of the blob text file.</param>
		/// <param name="data">The data you want to set the contents of the file to.</param>
		public static async void SetUserData(string googleID, string data)
		{
			CreateContainerNotExist();
			// Make clients
			BlobServiceClient serviceClient = new BlobServiceClient(BLOB_KEY);
			BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(BLOB_USERS_NAME);
			// Create temp file to upload
			
			// Upload file
			BlobClient blobClient = containerClient.GetBlobClient(googleID + ".txt");
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(data);
			writer.Flush();
			stream.Position = 0;
			await blobClient.UploadAsync(stream, true);
		}
	}
}
