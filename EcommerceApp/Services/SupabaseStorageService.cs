using System.Net.Http.Headers;

namespace EcommerceApp.Services
{
    // Sube archivos al Storage de Supabase y devuelve la URL pública.
    // Se usa tanto para subidas nuevas (desde el panel de Admin) como
    // para migrar imágenes que estaban guardadas localmente en wwwroot.
    public class SupabaseStorageService(HttpClient httpClient, IConfiguration configuration)
    {
        private readonly string _url = configuration["Supabase:Url"]!.TrimEnd('/');
        private readonly string _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]!;
        private readonly string _bucket = configuration["Supabase:Bucket"] ?? "products";

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            var path = $"{Guid.NewGuid()}-{fileName}".Replace(" ", "_");

            var requestUrl = $"{_url}/storage/v1/object/{_bucket}/{path}";

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Add("x-upsert", "true");

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al subir a Supabase Storage ({response.StatusCode}): {error}");
            }

            return $"{_url}/storage/v1/object/public/{_bucket}/{path}";
        }
    }
}
