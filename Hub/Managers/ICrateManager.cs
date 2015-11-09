﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Data.Crates;
using Newtonsoft.Json.Linq;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;

namespace Hub.Managers
{
    public interface ICrateStorageUpdater : IDisposable
    {
        CrateStorage  CrateStorage { get; set; }
        void DiscardChanges();
    }

    public interface ICrateManager
    {
        CrateDTO ToDto(Crate crate);
        CrateStorageDTO ToDto(CrateStorage storage);

        CrateStorage FromDto(CrateStorageDTO storageDto);
        Crate FromDto(CrateDTO crateDto);

        ICrateStorageUpdater UpdateStorage(Expression<Func<CrateStorageDTO>> storageAccessExpression);
        ICrateStorageUpdater UpdateStorage(Expression<Func<string>> storageAccessExpression);

        bool IsEmptyStorage(CrateStorageDTO storageDto);
        string EmptyStorageAsStr();

        Crate CreateAuthenticationCrate(string label, AuthenticationMode mode);
        Crate<StandardDesignTimeFieldsCM> CreateDesignTimeFieldsCrate(string label, params FieldDTO[] fields);
        Crate<StandardConfigurationControlsCM> CreateStandardConfigurationControlsCrate(string label, params ControlDefinitionDTO[] controls);
        Crate CreateStandardEventReportCrate(string label, EventReportCM eventReport);
        Crate CreateStandardEventSubscriptionsCrate(string label, params string[] subscriptions);
        Crate CreateStandardTableDataCrate(string label, bool firstRowHeaders, params TableRowDTO[] table);
        Crate CreatePayloadDataCrate(string payloadDataObjectType, string crateLabel, StandardTableDataCM tableDataMS);
        
    }
}
