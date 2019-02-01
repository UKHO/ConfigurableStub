using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UKHO.ConfigurableStub.Models;

namespace UKHO.ConfigurableStub
{
    public interface IStubManager
    {
        /// <summary>
        ///     Not sure if this is needed if its kicked off else where, we might just make the step that uses it a noop
        /// </summary>
        void Start();

        /// <summary>
        ///     Configures the  method at the specified {route} to return the given {statusCode} on requests that have a
        ///     certificate that matches the given {clientCertificateThumbPrint} and returns the (json serialized) given
        ///     {responsePayload}
        ///     Also records the Json that was sent to the controller as part of the request
        /// </summary>
        Task ConfigureRouteAsync(HttpVerb verb,
            string route,
            IEnumerable<string> requiredHeaders,
            string clientCertificateThumbPrint,
            HttpStatusCode statusCode,
            object responsePayload);

        Task ConfigureRouteAsync(HttpVerb verb,
            string route,
            IEnumerable<string> requiredHeaders,
            string clientCertificateThumbPrint,
            HttpStatusCode statusCode,
            string contentType,
            byte[] responsePayload);

        Task ConfigureRouteAsync(HttpVerb verb,
            string route,
            IEnumerable<string> requiredHeaders,
            string clientCertificateThumbPrint,
            HttpStatusCode statusCode);

        /// <summary>
        ///     Returns the JSON request that last was made to the given route deserialized as type T
        /// </summary>
        Task<RequestRecord<T>> LastRequestAsync<T>(HttpVerb verb, string route);
    }

    public enum HttpVerb
    {
        Get,
        Post,
        Delete
    }

    public class StubManager : IDisposable, IStubManager
    {
        private const string StubControlBaseUrl = "https://localhost:44310/stub";
        private PqcStubConsoleFacade stubConsole;
        private Process stubProcess;

        public StubManager()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
        }

        public void Dispose()
        {
            Close();
        }

        public void Start()
        {
            if (stubProcess != null)
                return;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Starting the Stub Stub...");
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                Arguments = "",
                FileName = FindStub(new FileInfo(GetType().Assembly.Location).Directory, 5)
            };

            stubProcess = new Process {StartInfo = startInfo};
            stubProcess.Start();
            stubConsole = new PqcStubConsoleFacade(stubProcess);

            TimingHelper.WaitFor(() =>
                stubConsole.Lines.Any(l => l.Contains("Now listening on: https://127.0.0.1:44310")));
            TimingHelper.WaitFor(() => stubConsole.Lines.Any(l => l.Contains("Application started.")));

            stopwatch.Stop();
            Console.WriteLine("Stub started after " + stopwatch.Elapsed);
        }

        public async Task ConfigureRouteAsync(HttpVerb verb,
            string route,
            IEnumerable<string> requiredHeaders,
            string clientCertificateThumbPrint,
            HttpStatusCode statusCode,
            object responsePayload)
        {
            await ConfigureRouteAsync(verb,
                route,
                new RouteConfiguration
                {
                    StatusCode = (int) statusCode,
                    Thumbprint = clientCertificateThumbPrint,
                    Response = JsonConvert.SerializeObject(responsePayload),
                    RequiredHeaders = requiredHeaders
                });
        }

        public async Task ConfigureRouteAsync(HttpVerb verb,
            string route,
            IEnumerable<string> requiredHeaders,
            string clientCertificateThumbPrint,
            HttpStatusCode statusCode,
            string responseContentType,
            byte[] responsePayload)
        {
            await ConfigureRouteAsync(verb,
                route,
                new RouteConfiguration
                {
                    StatusCode = (int) statusCode,
                    Thumbprint = clientCertificateThumbPrint,
                    Base64EncodedBinaryResponse = Convert.ToBase64String(responsePayload),
                    ContentType = responseContentType,
                    RequiredHeaders = requiredHeaders
                });
        }

        public Task ConfigureRouteAsync(HttpVerb verb,
            string route,
            IEnumerable<string> requiredHeaders,
            string clientCertificateThumbPrint,
            HttpStatusCode statusCode)
        {
            return ConfigureRouteAsync(verb, route, requiredHeaders, clientCertificateThumbPrint, statusCode, null);
        }

        public async Task<RequestRecord<T>> LastRequestAsync<T>(HttpVerb verb, string route)
        {
            if (stubProcess == null)
                Start();

            var client = new HttpClient();
            var requestUri = $"{StubControlBaseUrl}/{verb}/{route}";
            var response = await client.GetAsync(requestUri);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new WebException(
                    $"Failed to get last request from the stub. Stub returned {(int) response.StatusCode} {response.ReasonPhrase}\n{responseBody}");

            var result = JsonConvert.DeserializeObject<RequestRecord<T>>(responseBody);
            return result;
        }

        private string FindStub(DirectoryInfo dir, int maxSearchUp)
        {
            bool ExcludeObj(FileSystemInfo f)
            {
                return !f.FullName.Contains("obj");
            }

            var stubFileName = "UKHO.ConfigurableStub.Stub.exe";
            var stubExeFiles = dir.EnumerateFileSystemInfos(stubFileName, SearchOption.AllDirectories).ToArray();
            if (stubExeFiles.Any(ExcludeObj))
                return stubExeFiles.First(ExcludeObj).FullName;
            if (maxSearchUp > 0 && dir.Parent != null)
                return FindStub(dir.Parent, maxSearchUp - 1);
            throw new FileNotFoundException($"Can't find {stubFileName} in {dir.FullName}");
        }

        private void Close()
        {
            if (stubProcess == null || stubProcess.HasExited)
                return;

            stubProcess.StandardInput.Write(new[] {(char) 3}); // Send Ctrl+C to request shutdown.
            // ReSharper disable once AccessToDisposedClosure
            TimingHelper.WaitFor(() => stubProcess.HasExited, throwExceptionIfPredicateNotTrue: false);
            if (!stubProcess.HasExited)
                stubProcess.Kill();

            stubConsole?.Dispose();
            stubProcess.Dispose();
            stubProcess = null;
            stubConsole = null;
        }

        private async Task ConfigureRouteAsync(HttpVerb verb, string route, RouteConfiguration configuration)
        {
            if (stubProcess == null)
                Start();

            var client = new HttpClient();
            var requestUri = $"{StubControlBaseUrl}/{verb}/{route}";

            var response =
                await client.PostAsync(requestUri, new StringContent(JsonConvert.SerializeObject(configuration)));
            response.EnsureSuccessStatusCode();
        }
    }
}