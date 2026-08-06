using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace LaCasitaDeMiga.Features.Common.Image.services {
    public class ImageServiceImpl : IImageService {
        private readonly Cloudinary _cloudinary;

        public ImageServiceImpl(IConfiguration config) {
            // Buscamos las credenciales en appsettings.json
            var cloudName = config["Cloudinary:CloudName"];
            var apiKey = config["Cloudinary:ApiKey"];
            var apiSecret = config["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret)) {
                throw new InvalidOperationException("Faltan las credenciales de Cloudinary en la configuración.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file, string folder) {
            if (file == null || file.Length == 0) {
                throw new ArgumentException("El archivo está vacío o es nulo.");
            }

            var uploadResult = new ImageUploadResult();

            using (var stream = file.OpenReadStream()) {
                var uploadParams = new ImageUploadParams {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder, // Agrupa las fotos en Cloudinary
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto") // Optimización automática de peso y formato!
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null) {
                throw new Exception($"Error al subir la imagen a Cloudinary: {uploadResult.Error.Message}");
            }

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }

        public async Task<bool> DeleteAsync(string publicId) {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }
    }
}
