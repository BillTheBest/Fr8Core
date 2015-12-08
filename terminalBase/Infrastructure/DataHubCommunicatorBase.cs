﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Managers;
using StructureMap;

namespace TerminalBase.Infrastructure
{
    public abstract class DataHubCommunicatorBase : IHubCommunicator
    {
        public ICrateManager Crate { get; set; }

        protected DataHubCommunicatorBase()
        {
            Crate = ObjectFactory.GetInstance<ICrateManager>();
        }

        protected abstract string LabelPrefix { get; }

        private void StripLabelPrefix(IEnumerable<Crate> crates, string prefix)
        {
            foreach (var crate in crates)
            {
                if (crate.Label != prefix && crate.Label.StartsWith(prefix + "_"))
                {
                    crate.Label = crate.Label.Substring((prefix + "_").Length);
                }
            }
        }

        public Task<PayloadDTO> GetProcessPayload(ActionDO actionDO, Guid containerId)
        {
            var payload = new PayloadDTO(containerId)
            {
                CrateStorage = new CrateStorageDTO()
            };

            var crateStorage = Crate.GetStorage(actionDO);
            using (var updater = Crate.UpdateStorage(payload))
            {
                var crates = crateStorage
                    .Where(x => x.Label.StartsWith(LabelPrefix + "_PayloadCrate"))
                    .ToList();

                StripLabelPrefix(crates, LabelPrefix + "_PayloadCrate");

                updater.CrateStorage.AddRange(crates);
            }

            return Task.FromResult(payload);
        }

        public Task<List<Crate<TManifest>>> GetCratesByDirection<TManifest>(
            ActionDO actionDO, CrateDirection direction)
        {
            var searchLabel = direction == CrateDirection.Upstream
                ? LabelPrefix + "_UpstreamCrate"
                : LabelPrefix + "_DownstreamCrate";

            var crateStorage = Crate.GetStorage(actionDO);
            var crates = crateStorage
                .CratesOfType<TManifest>(x => x.Label.StartsWith(searchLabel))
                .ToList();

            StripLabelPrefix(crates, searchLabel);

            return Task.FromResult(crates);
        }

        public Task<List<Crate>> GetCratesByDirection(ActionDO actionDO, CrateDirection direction)
        {
            var searchLabel = direction == CrateDirection.Upstream
                ? LabelPrefix + "_UpstreamCrate"
                : LabelPrefix + "_DownstreamCrate";

            var crateStorage = Crate.GetStorage(actionDO);
            var crates = crateStorage
                .Where(x => x.Label.StartsWith(searchLabel))
                .ToList();

            StripLabelPrefix(crates, searchLabel);

            return Task.FromResult(crates);
        }

        public Task<List<ActivityTemplateDTO>> GetActivityTemplates(ActionDO actionDO)
        {
            var searchLabel = LabelPrefix + "_ActivityTemplate";

            var crateStorage = Crate.GetStorage(actionDO);
            var activityTemplates = crateStorage
                .Where(x => x.Label == searchLabel)
                .Select(x => x.Get<ActivityTemplateDTO>())
                .ToList();

            return Task.FromResult(activityTemplates);
        }

        public async Task<List<ActivityTemplateDTO>> GetActivityTemplates(
            ActionDO actionDO, ActivityCategory category)
        {
            var allTemplates = await GetActivityTemplates(actionDO);
            var activityTemplates = allTemplates
                .Where(x => x.Category == category)
                .ToList();

            return activityTemplates;
        }
    }
}
