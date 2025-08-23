using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Extensions.Http
{
    public static class HttpRequestDataExtensions
    {
        /// <summary>
        /// 200 OK + JSON
        /// </summary>
        public static async Task<HttpResponseData> OkAsJsonAsync(
            this HttpRequestData req,
            object content)
        {
            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(content ?? new object());
            return res;
        }

        /// <summary>
        /// 201 Created + JSON (y Location header opcional)
        /// </summary>
        public static async Task<HttpResponseData> CreatedAsJsonAsync(
            this HttpRequestData req,
            object content,
            string? location = null)
        {
            var res = req.CreateResponse(HttpStatusCode.Created);
            if (!string.IsNullOrEmpty(location))
                res.Headers.Add("Location", location);
            await res.WriteAsJsonAsync(content ?? new object());
            return res;
        }

        /// <summary>
        /// 204 No Content
        /// </summary>
        public static Task<HttpResponseData> NoContentAsync(
            this HttpRequestData req)
        {
            var res = req.CreateResponse(HttpStatusCode.NoContent);
            return Task.FromResult(res);
        }

        /// <summary>
        /// 400 Bad Request + texto
        /// </summary>
        public static async Task<HttpResponseData> BadRequestAsync(
            this HttpRequestData req,
            string message)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync(message);
            return res;
        }

        /// <summary>
        /// 404 Not Found + texto
        /// </summary>
        public static async Task<HttpResponseData> NotFoundAsync(
            this HttpRequestData req,
            string message)
        {
            var res = req.CreateResponse(HttpStatusCode.NotFound);
            await res.WriteStringAsync(message);
            return res;
        }

        /// <summary>
        /// 500 Internal Server Error + texto
        /// </summary>
        public static async Task<HttpResponseData> ErrorAsync(
            this HttpRequestData req,
            string message)
        {
            var res = req.CreateResponse(HttpStatusCode.InternalServerError);
            await res.WriteStringAsync(message);
            return res;
        }

        /// <summary>
        /// Genérico: cualquier status + JSON
        /// </summary>
        public static async Task<HttpResponseData> AsJsonAsync(
            this HttpRequestData req,
            object content,
            HttpStatusCode status)
        {
            var res = req.CreateResponse(status);
            await res.WriteAsJsonAsync(content ?? new object());
            return res;
        }
    }
}
