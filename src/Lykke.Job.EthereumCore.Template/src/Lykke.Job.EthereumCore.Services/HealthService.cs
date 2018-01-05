using System.Collections.Generic;
using Lykke.Job.EthereumCore.Core.Domain.Health;
using Lykke.Job.EthereumCore.Core.Services;

namespace Lykke.Job.EthereumCore.Services
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    public class HealthService : IHealthService
    {
        // TODO: Feel free to add properties, which contains your helath metrics, and use it in monitoring layer or in IsAlive API endpoint

        public string GetHealthViolationMessage()
        {
            // TODO: Check gathered health statistics, and return appropriate health violation message, or NULL if job hasn't critical errors
            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            // TODO: Check gathered health statistics, and add appropriate health issues message to issues

            return issues;
        }

        // TODO: Place health tracing methods here
    }
}