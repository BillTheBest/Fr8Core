/// <reference path="../_all.ts" />

/*
    The service enables operations with Process Templates
*/
module dockyard.services {
    export interface IRouteService extends ng.resource.IResourceClass<interfaces.IRouteVM> {
        getbystatus: (id: { id: number; status: number; }) => Array<interfaces.IRouteVM>;
        getFull: (id: Object) => interfaces.IRouteVM;
        getByAction: (id: { id: number }) => interfaces.IRouteVM;
        execute: (id: { id: number }) => void;
        activate: (processTemplate: model.RouteDTO) => void;
        deactivate: (processTemplate: model.RouteDTO) => void;
        update: (data: { id: number, name: string}) => interfaces.IRouteVM;
    }

    export interface IActionService extends ng.resource.IResourceClass<interfaces.IActionVM> {
        configure: (action: interfaces.IActionDTO) => ng.resource.IResource<interfaces.IActionVM>;
        getByProcessTemplate: (id: Object) => ng.resource.IResource<Array<interfaces.IActionVM>>;
        create: (args: { actionTemplateId: number, name: string, label: string, parentNideId: number, createRoute: boolean }) => ng.resource.IResource<model.RouteDTO | model.ActionDTO>;
        createSolution: (args: { solutionName: string }) => ng.resource.IResource<model.RouteDTO>;
        //TODO make resource class do this operation
        deleteById: (id: { id: number; confirmed: boolean }) => ng.resource.IResource<string>;
        
        //getFieldDataSources: (params: Object, data: interfaces.IActionVM) => interfaces.IDataSourceListVM;
    }

    export interface IDocuSignTemplateService extends ng.resource.IResourceClass<interfaces.IDocuSignTemplateVM> { }

    export interface IDocuSignTriggerService extends ng.resource.IResourceClass<interfaces.IDocuSignExternalEventVM> { }

    interface __ISubrouteService extends ng.resource.IResourceClass<interfaces.ISubrouteVM> {
        add: (curProcessNodeTemplate: model.SubrouteDTO) => interfaces.ISubrouteVM;
        update: (curProcessNodeTemplate: model.SubrouteDTO) => interfaces.ISubrouteVM;
    }

    interface __ICriteriaService extends ng.resource.IResourceClass<interfaces.ICriteriaVM> {
        update: (curCriteria: model.CriteriaDTO) => interfaces.ICriteriaVM;
        byProcessNodeTemplate: (id: { id: number }) => interfaces.ICriteriaVM;
    }

    export interface ICriteriaServiceWrapper {
        load: (id: number) => ng.IPromise<model.SubrouteDTO>;
        add: (curProcessNodeTemplate: model.SubrouteDTO) => ng.IPromise<model.SubrouteDTO>;
        update: (curProcessNodeTemplate: model.SubrouteDTO) => ng.IPromise<model.SubrouteDTO>;
        addOrUpdate(curProcessNodeTemplate: model.SubrouteDTO): {
            actionType: ActionTypeEnum;
            promise: ng.IPromise<model.SubrouteDTO>
        }
    }

    export interface IProcessBuilderService {
        saveCurrent(current: model.ProcessBuilderState): ng.IPromise<model.ProcessBuilderState>
    }

    export interface IActivityTemplateService extends ng.resource.IResourceClass<interfaces.IActivityTemplateVM> {
        getAvailableActivities: () => ng.resource.IResource<Array<interfaces.IActivityCategoryDTO>>;
    }

  

    /*
        ProcessTemplateDTO CRUD service.
    */

    app.factory('RouteService', ['$resource', ($resource: ng.resource.IResourceService): IRouteService =>
        <IRouteService>$resource('/routes/:id', { id: '@id' },
            {
                'getFull': {
                    method: 'GET',
                    isArray: false,
                    url: '/routes/full/:id',
                    params: {
                        id: '@id'
                    }
                },
                'getbystatus': {
                    method: 'GET',
                    isArray: true,
                    url: '/routes/status?status=:status',
                    params: {
                        status: '@status'
                    }
                },
                'getByAction': {
                    method: 'GET',
                    isArray: false,
                    url: '/routes/getByAction/:id',
                    params: {
                        id: '@id'
                    }
                },
                'execute': {
                    method: 'POST',
                    isArray: false,
                    url: '/routes/run?routeId=:id',
                    params: {
                        id: '@id'
                    }
                },
                'activate': {
                    method: 'POST',
                    url: '/routes/activate/',
                    params: {
                    }
                },
                'deactivate': {
                    method: 'POST',
                    url: '/routes/deactivate/',
                    params: {
                    }
                },
                'update': {
                    method: 'POST',
                    url: '/routes/',
                    params: {

                    }
                }
            })
    ]);

    /*
        DocuSignTemplateDTO CRUD service.
    */
    app.factory('DocuSignTemplateService', ['$resource', ($resource: ng.resource.IResourceService): IDocuSignTemplateService =>
        <IDocuSignTemplateService>$resource('/api/docusigntemplate')
    ]);

    /* 
        DocuSignExternalEventDTO CRUD service.
    */
    app.factory('DocuSignTriggerService', ['$resource', ($resource: ng.resource.IResourceService): IDocuSignTriggerService =>
        <IDocuSignTriggerService>$resource('/route/triggersettings')
    ]);

    app.factory('ActionTemplateService', ['$resource', ($resource: ng.resource.IResourceService): IActionService =>
        <IActionService>$resource('/actiontemplates/', null,
            {
                'available': {
                    method: 'GET',
                    isArray: true,
                    url: '/route_nodes/available',
                    params: {
                        tag: '@tag'
                    }
                }
            })
    ]);

    /* 
        ActionDTO CRUD service.
    */
    app.factory('ActionService', ['$resource', ($resource: ng.resource.IResourceService): IActionService =>
        <IActionService>$resource('/actions/:id',
            {
                id: '@id'
            },
            {
                'save': {
                    method: 'POST',
                    isArray: false,
                    url: '/actions/save',
                    params: {
                        suppressSpinner: true // Do not show page-level spinner since we have one within the Configure Action pane
                    }

                },
                //'get': {
                //    transformResponse: function (data) {
                //        //Map a proto-action object to an actual ActionDesignDTO instance so that we can access methods 
                //        var dataObject: interfaces.IActionDesignDTO = angular.fromJson(data);
                //        return model.ActionDesignDTO.create(dataObject);
                //    }
                //},
                'delete': { method: 'DELETE' },
                'configure': {
                    method: 'POST',
                    url: '/actions/configure',
                    params: {
                        suppressSpinner: true // Do not show page-level spinner since we have one within the Configure Action pane
                    }
                },
                'getByProcessTemplate': {
                    method: 'GET',
                    url: '/actions/bypt',
                    isArray: true
                },
                'deleteById': {
                    method: 'DELETE',
                    url: '/actions/:id?confirmed=:confirmed'
                },
                'create': {
                    method: 'POST',
                    url: '/actions/create'
                },
                'createSolution': {
                    method: 'POST',
                    url: '/actions/create',
                    params: {
                        solutionName: '@solutionName'
                    }
                },
                'params': {
                    id: 'id'
                }
            })
    ]);

    /* 
        CriteriaDTO CRUD service.
        This service is not intended to be used by anything except CriteriaServiceWrapper,
        that's why its name starts with underscores. 
    */
    app.factory('__CriteriaService', ['$resource', ($resource: ng.resource.IResourceService): __ICriteriaService =>
        <__ICriteriaService>$resource('/api/criteria', null,
            {
                'update': {
                    method: 'PUT'
                },
                'save': {
                    method: 'PUT'
                },
                'byProcessNodeTemplate': {
                    method: 'GET',
                    url: '/api/criteria/byProcessNodeTemplate'
                }
            })
    ]);

    /* 
        ProcessNodeTemplateDTO CRUD service.
        This service is not intended to be used by anything except CriteriaServiceWrapper,
        that's why its name starts with underscores. 
    */
    app.factory('__SubrouteService', ['$resource', 'urlPrefix', ($resource: ng.resource.IResourceService): __ISubrouteService =>
        <__ISubrouteService>$resource('/api/processnodetemplate', null,
            {
                'add': {
                    method: 'POST'
                },
                'update': {
                    method: 'PUT'
                }
            })
    ]);

    app.factory('ActivityTemplateService', ['$resource', ($resource: ng.resource.IResourceService): IActivityTemplateService =>
        <IActivityTemplateService>$resource('/api/activityTemplates/:id', { id: '@id' },
            {
                'getAvailableActivities': {
                    method: 'GET',
                    url: '/route_nodes/available/',
                    isArray: true
                }
            })
    ]);

    /*
        General data persistance methods for ProcessBuilder.
    */
    class ProcessBuilderService implements IProcessBuilderService {
        constructor(
            private $q: ng.IQService,
            private CriteriaServiceWrapper: ICriteriaServiceWrapper,
            private ActionService: IActionService,
            private crateHelper: CrateHelper
        ) { }

        /* 
            The function saves current entities if they are new or changed (dirty).
            At this time not all entities whose state we maintain on ProcessBuilder are saved here. 
            I (@alexavrutin) will add them one-by-one during the course of refactoring. 
            Dirty checking is missing at this moment, too, I will add it later. Now it saves entities no matter 
            if they were or were not changed. 
        */
        public saveCurrent(currentState: model.ProcessBuilderState): ng.IPromise<model.ProcessBuilderState> {
            var deferred = this.$q.defer<model.ProcessBuilderState>(),
                newState = new model.ProcessBuilderState()

            // TODO: bypass save for unchanged entities
              
            // Save processNodeTemplate if not null
            if (currentState.subroute) {
                this.CriteriaServiceWrapper.addOrUpdate(currentState.subroute).promise
                    .then((result: interfaces.ISubrouteVM) => {
                        //new model.CriteriaDTO(result.criteria.id, false, result.criteria.id, model.CriteriaExecutionType.NoSet);
                        newState.subroute = result;

                        this.crateHelper.mergeControlListCrate(
                            currentState.action.configurationControls,
                            currentState.action.crateStorage
                        );

                        // If an Action is selected, save it
                        if (currentState.action) {
                            return this.ActionService.save({ id: currentState.action.id },
                                currentState.action, null, null);
                        }
                        else {
                            return deferred.resolve(newState);
                        }
                    })
                    .then((result: interfaces.IActionVM) => {
                        newState.action = result;
                        return deferred.resolve(newState);
                    })
                    .catch((reason: any) => {
                        return deferred.reject(reason);
                    });
            }

            //Save Action only
            else if (currentState.action) {
                this.crateHelper.mergeControlListCrate(
                    currentState.action.configurationControls,
                    currentState.action.crateStorage
                );

                var promise = this.ActionService.save(
                    { id: currentState.action.id },
                    currentState.action,
                    null,
                    null).$promise;
                promise
                    .then((result: interfaces.IActionVM) => {
                        newState.action = result;
                        return deferred.resolve(newState);
                    })
                    .catch((reason: any) => {
                        return deferred.reject(reason);
                    });
            }
            else {
                //Nothing to save
                deferred.resolve(newState);
            }
            return deferred.promise;
        }
    }

    /*
        Register ProcessBuilderService with AngularJS
    */
    app.factory('ProcessBuilderService', ['$q', 'CriteriaServiceWrapper', 'ActionService', 'CrateHelper', 'UIHelperService', (
        $q: ng.IQService,
        CriteriaServiceWrapper: ICriteriaServiceWrapper,
        ActionService: IActionService,
        crateHelper: CrateHelper) => {
        return new ProcessBuilderService($q, CriteriaServiceWrapper, ActionService, crateHelper);
    }
    ]);

    /*
        A Service which is trying to encapsulate the fact that working with a Criteria in UI 
        involves updating two entities: CriteriaDTO and ProcessNodeTemplateDTO. 
        The name is not good but I could not come up with a better one. 
        If we combined CriteriaDTO and ProcessNodeTemplateDTO, this complexity would not be necessary.
    */
    class CriteriaServiceWrapper implements ICriteriaServiceWrapper {
        public constructor(
            private CriteriaService: __ICriteriaService,
            private SubrouteService: __ISubrouteService,
            private $q: ng.IQService) {
        }

        public add(curProcessNodeTemplate: model.SubrouteDTO): ng.IPromise<model.SubrouteDTO> {
            var deferred = this.$q.defer<interfaces.ISubrouteVM>();

            // Save ProcessNodeTemplate object to server.
            // Server automatically creates empty criteria node.
            var addDeferred = this.SubrouteService.add(curProcessNodeTemplate);

            addDeferred.$promise
                .then((addResult: interfaces.ISubrouteVM) => {
                    curProcessNodeTemplate.isTempId = false;
                    curProcessNodeTemplate.id = addResult.id;    
                    // Fetch criteria object from server by ProcessNodeTemplate global ID.
                    return this.CriteriaService.byProcessNodeTemplate({ id: addResult.id }).$promise;
                })
                .then((getResult: interfaces.ICriteriaVM) => {
                    curProcessNodeTemplate.criteria.id = getResult.id;
                    curProcessNodeTemplate.criteria.isTempId = false;
                    // Update criteria object on server.
                    return this.CriteriaService.save({ id: getResult.id }).$promise;
                })
                .then((updateResult: interfaces.ICriteriaVM) => {
                    deferred.resolve(addDeferred);
                })
                .catch((reason: any) => {
                    deferred.reject(reason);
                });

            return deferred.promise;
        }

        public update(curProcessNodeTemplate: model.SubrouteDTO): ng.IPromise<model.SubrouteDTO> {
            var deferred = this.$q.defer<interfaces.ISubrouteVM>();

            // Save ProcessNodeTemplate object to server.
            // Server automatically creates empty criteria node.
            var updateDeferred = this.SubrouteService.update(curProcessNodeTemplate);

            updateDeferred.$promise
                .then((updateResult: interfaces.ISubrouteVM) => {   
                    // Call REST api to update Criteria entity on server.
                    return this.CriteriaService.update(curProcessNodeTemplate.criteria).$promise;
                })
                .then((updateResult: interfaces.ICriteriaVM) => {
                    deferred.resolve(updateDeferred);
                })
                .catch((reason: any) => {
                    deferred.reject(reason);
                });

            return deferred.promise;
        }

        public load(id: number): ng.IPromise<model.SubrouteDTO> {
            var deferred = this.$q.defer<interfaces.ISubrouteVM>();

            // Save ProcessNodeTemplate object to server.
            // Server automatically creates empty criteria node.
            var getPntDeferred = this.SubrouteService.get({ id: id }),
                getCriteriaDeferred = this.CriteriaService.byProcessNodeTemplate({ id: id });

            this.$q.all([getPntDeferred.$promise, getCriteriaDeferred.$promise])
                .then(() => {
                    var criteria = new model.CriteriaDTO(
                        getCriteriaDeferred.id,
                        false,
                        getPntDeferred.id,
                        getCriteriaDeferred.executionType
                    );

                    angular.forEach(getCriteriaDeferred.conditions, function (it: model.Condition) {
                        criteria.conditions.push(it);
                    });

                    getPntDeferred.criteria = criteria;
                    deferred.resolve(getPntDeferred);
                });

            return deferred.promise;
        }

        /*
            This method does adding or updating depending on whether 
            ProcessNodeTemplate has been saved or not.
        */
        public addOrUpdate(curProcessNodeTemplate: model.SubrouteDTO): {
            actionType: ActionTypeEnum,
            promise: ng.IPromise<model.SubrouteDTO>
        } {
            // Don't save anything if there is no criteria selected, 
            // just return a null-valued resolved promise
            if (!curProcessNodeTemplate) {
                var deferred = this.$q.defer<model.SubrouteDTO>();
                deferred.resolve(null);
                return {
                    actionType: ActionTypeEnum.None,
                    promise: deferred.promise
                };
            }

            if (curProcessNodeTemplate.isTempId) {
                // In case of newly created object (i.e. isTempId === true).
                return {
                    actionType: ActionTypeEnum.Add,
                    promise: this.add(curProcessNodeTemplate)
                };
            }
            else {
                return {
                    actionType: ActionTypeEnum.Update,
                    promise: this.update(curProcessNodeTemplate)
                };
            }
        }

    }

    export enum ActionTypeEnum {
        None = 0,
        Add = 1,
        Update = 2
    }

    /*
        Register CriteriaServiceWrapper with AngularJS.
    */
    app.factory('CriteriaServiceWrapper', ['__CriteriaService', '__SubrouteService', '$q',
        (CriteriaService, SubrouteService, $q) => {
            return new CriteriaServiceWrapper(CriteriaService, SubrouteService, $q)
        }]);


}