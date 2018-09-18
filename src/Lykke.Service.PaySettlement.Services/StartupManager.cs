using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Sdk;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.PaySettlement.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly ICqrsEngine _cqrsEngine;
        private readonly ILog _log;

        public StartupManager([NotNull] ICqrsEngine cqrsEngine, 
            [NotNull] ILogFactory logFactory)
        {
            _cqrsEngine = cqrsEngine ?? throw new ArgumentNullException(nameof(cqrsEngine));
            _log = logFactory.CreateLog(this);
        }

        public Task StartAsync()
        {
            StartComponent(_cqrsEngine);
            return Task.CompletedTask;
        }

        private void StartComponent(ICqrsEngine cqrsEngine)
        {
            _log.Info($"Starting {nameof(cqrsEngine)} ...");
            cqrsEngine.Start();
            _log.Info($"{nameof(cqrsEngine)} successfully started.");
        }
    }
}
