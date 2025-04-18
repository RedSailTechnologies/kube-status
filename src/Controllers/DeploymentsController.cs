﻿using System.Threading.Tasks;
using KubeStatus.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KubeStatus.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class DeploymentsController(DeploymentService deploymentService) : ControllerBase
    {
        private readonly DeploymentService _deploymentService = deploymentService;

        [HttpGet]
        public IActionResult ListNamespacedDeploymentAsync()
        {
            return Redirect("/api/Deployments/default");
        }

        [HttpGet("{k8sNamespace}")]
        public async Task<IActionResult> GetAllNamespacedDeploymentsAsync(string k8sNamespace = "default")
        {
            k8s.Models.V1DeploymentList? deployments = await _deploymentService.GetAllNamespacedDeploymentsAsync(k8sNamespace);

            if (deployments == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(deployments);
            }
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("restart/{name}")]
        public async Task<IActionResult> RestartDeploymentAsync(string name, string k8sNamespace = "default")
        {
            bool restarted = await _deploymentService.RestartDeploymentAsync(name, k8sNamespace);

            if (restarted)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("rollout/restart")]
        public async Task<IActionResult> RestartNamespacedDeploymentAsync(string k8sNamespace = "default")
        {
            bool restarted = await _deploymentService.RestartNamespacedDeploymentAsync(k8sNamespace);

            if (restarted)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }

        [Authorize(Policy = "RequireEditorRole")]
        [HttpPatch("scale/{name}/{replicas}")]
        public async Task<IActionResult> ScaleDeploymentAsync(string name, int replicas, string k8sNamespace = "default")
        {
            bool scaled = await _deploymentService.ScaleDeploymentAsync(name, replicas, k8sNamespace);

            if (scaled)
            {
                return Ok();
            }
            else
            {
                return Problem();
            }
        }
    }
}
