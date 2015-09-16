﻿module dockyard.services {

    export interface IReportFactService extends ng.resource.IResourceClass<interfaces.IReportFactVM> { }

    export interface IReportIncidentService extends ng.resource.IResourceClass<interfaces.IReportIncidentVM> { }


    app.factory('ReportFactService', ['$resource', ($resource: ng.resource.IResourceService): IReportFactService =>
        <IReportFactService> $resource('/report/GetFacts', {})
    ]);

    app.factory('ReportIncidentService', ['$resource', ($resource: ng.resource.IResourceService): IReportIncidentService =>
        <IReportIncidentService> $resource('/report/GetIncidents', {})
    ]);
} 