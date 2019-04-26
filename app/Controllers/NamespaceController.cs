using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubernetesWeb.Models;
using System.IO;
using System;
using System.Net;

namespace KubernetesWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NamespaceController : ControllerBase
    {
        private IKubernetes _kubernetesClient;

        public NamespaceController()
        {
            // For Development
            // FileInfo kubeConfigFileInfo = new FileInfo("../../.kube/config");
            // var k8SClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigFileInfo);

            // For running on K8S cluster
            var k8SClientConfig = KubernetesClientConfiguration.InClusterConfig();
            _kubernetesClient = new Kubernetes(k8SClientConfig);
        }

        // GET: api/namespace
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Namespace>>> GetNamespaces()
        {
            var v1NamespaceList = _kubernetesClient.ListNamespace();

            List<Namespace> k8snamespaces = new List<Namespace>();

            foreach (var item in v1NamespaceList.Items)
            {
                Namespace k8snamespace = new Namespace();
                k8snamespace.Name = item.Metadata.Name;

                k8snamespaces.Add(k8snamespace);
            }

            return k8snamespaces;
        }

        // GET: api/namespace/default
        [HttpGet("{name}")]
        public async Task<ActionResult<Namespace>> GetNamespace(string name)
        {
            var v1NamespaceList = _kubernetesClient.ListNamespace();

            var v1Namespace = v1NamespaceList.Items.FirstOrDefault<V1Namespace>(item => item.Metadata.Name == name);

            if (v1Namespace == null)
            {
                return NotFound();
            }

            Namespace k8snamespace = new Namespace() { Name = v1Namespace.Metadata.Name };

            return k8snamespace;
        }

        [HttpPost]
        public async Task<ActionResult<Namespace>> PostNamespace(Namespace k8snamespace)
        {
            if (k8snamespace == null)
            {
                return BadRequest();
            }
            else
            {
                var ns = new V1Namespace
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = k8snamespace.Name
                    }
                };

                var result = _kubernetesClient.CreateNamespace(ns);

                if (result == null)
                {
                    return null;
                }
                else
                {
                    return CreatedAtAction(nameof(GetNamespace), new { Name = k8snamespace.Name }, k8snamespace);
                }
            }
        }

        // DELETE: api/namespace/default
        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteNamespace(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest();
            }
            else
            {
                var status = _kubernetesClient.DeleteNamespace(name, new V1DeleteOptions());

                if (status.HasObject)
                {
                    var obj = status.ObjectView<V1Namespace>();
                    Console.WriteLine(obj.Status.Phase);

                    await Delete(_kubernetesClient, name, 3 * 1000);
                }
                
                return NoContent();
            }
        }

        private async Task DeleteAsync(IKubernetes client, string name, int delayMillis)
        {
            while (true)
            {
                await Task.Delay(delayMillis);
                try
                {
                    await client.ReadNamespaceAsync(name);
                }
                catch (AggregateException ex)
                {
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        if (innerEx is Microsoft.Rest.HttpOperationException)
                        {
                            var code = ((Microsoft.Rest.HttpOperationException)innerEx).Response.StatusCode;
                            if (code == HttpStatusCode.NotFound)
                            {
                                return;
                            }
                            throw ex;
                        }
                    }
                }
                catch (Microsoft.Rest.HttpOperationException ex)
                {
                    if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return;
                    }
                    throw ex;
                }
            }
        }

        private async Task Delete(IKubernetes client, string name, int delayMillis)
        {
            await DeleteAsync(client, name, delayMillis);
        }
    }
}