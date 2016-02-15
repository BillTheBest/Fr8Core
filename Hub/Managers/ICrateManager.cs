﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;

namespace Hub.Managers
{
    public interface IUpdatableCrateStorage : IDisposable, ICrateStorage
    {
        void Replace(ICrateStorage crateStorage);
        void DiscardChanges();
    }

    public interface ICrateManager
    {
        CrateDTO ToDto(Crate crate);
        CrateStorageDTO ToDto(ICrateStorage storage);

        ICrateStorage FromDto(CrateStorageDTO storageDto);
        Crate FromDto(CrateDTO crateDto);

        IUpdatableCrateStorage UpdateStorage(Expression<Func<CrateStorageDTO>> storageAccessExpression);
        IUpdatableCrateStorage UpdateStorage(Expression<Func<string>> storageAccessExpression);

        bool IsEmptyStorage(CrateStorageDTO storageDto);
        string EmptyStorageAsStr();
        string CrateStorageAsStr(ICrateStorage storage);
        void AddLogMessage(string label, List<LogItemDTO> logItemList, ContainerDO containerDO);
        Crate CreateAuthenticationCrate(string label, AuthenticationMode mode);

        Crate<ManifestDescriptionCM> CreateManifestDescriptionCrate(string label, string name, string id, AvailabilityType availability);
        Crate<StandardDesignTimeFieldsCM> CreateDesignTimeFieldsCrate(string label, params FieldDTO[] fields);
        Crate<StandardDesignTimeFieldsCM> CreateDesignTimeFieldsCrate(string label, List<FieldDTO> fields);
        Crate<StandardDesignTimeFieldsCM> CreateDesignTimeFieldsCrate(string label, List<FieldDTO> fields, AvailabilityType availability);
        Crate<StandardDesignTimeFieldsCM> CreateDesignTimeFieldsCrate(string label, AvailabilityType availability, params FieldDTO[] fields);
        Crate<StandardConfigurationControlsCM> CreateStandardConfigurationControlsCrate(string label, params ControlDefinitionDTO[] controls);
        Crate CreateStandardEventReportCrate(string label, EventReportCM eventReport);
        Crate CreateStandardEventSubscriptionsCrate(string label, string manufacturer, params string[] subscriptions);
        Crate CreateStandardTableDataCrate(string label, bool firstRowHeaders, params TableRowDTO[] table);
        Crate CreatePayloadDataCrate(string payloadDataObjectType, string crateLabel, StandardTableDataCM tableDataMS);
        Crate CreateOperationalStatusCrate(string label, OperationalStateCM eventReport);
        StandardPayloadDataCM TransformStandardTableDataToStandardPayloadData(string curObjectType, StandardTableDataCM tableDataMS);
        string GetFieldByKey<T>(CrateStorageDTO curCrateStorage, string findKey) where T : Manifest;
        T GetByManifest<T>(PayloadDTO payloadDTO) where T : Manifest;
        OperationalStateCM GetOperationalState(PayloadDTO payloadDTO);
        IEnumerable<FieldDTO> GetFields(IEnumerable<Crate> crates);
        IEnumerable<string> GetLabelsByManifestType(IEnumerable<Crate> crates, string manifestType);
        StandardDesignTimeFieldsCM MergeContentFields(List<Crate<StandardDesignTimeFieldsCM>> curCrates);
        T GetContentType<T>(string crate) where T : class;
    }
}
