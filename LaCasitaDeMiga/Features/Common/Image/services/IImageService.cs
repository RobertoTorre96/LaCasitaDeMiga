namespace LaCasitaDeMiga.Features.Common.Image.services {
    public interface IImageService {

        /// <summary>
        /// Sube una imagen a Cloudinary.
        /// </summary>
        /// <param name="file">El archivo físico que viene del Frontend.</param>
        /// <param name="folder">La carpeta dentro de Cloudinary donde se guardará (ej: "productos").</param>
        /// <returns>Una tupla con la URL segura y el Public ID.</returns>
        Task<(string Url, string PublicId)> UploadAsync(IFormFile file, string folder);

        /// <summary>
        /// Borra una imagen de Cloudinary usando su Public ID.
        /// </summary>
        Task<bool> DeleteAsync(string publicId);

    }
}
