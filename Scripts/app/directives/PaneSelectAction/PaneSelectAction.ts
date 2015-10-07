﻿/// <reference path="../../_all.ts" />

module dockyard.directives.paneSelectAction {
    'use strict';

    import pwd = dockyard.directives.paneWorkflowDesigner;

    export enum MessageType {
        PaneSelectAction_ActionUpdated,
        PaneSelectAction_Render,
        PaneSelectAction_Hide,
        PaneSelectAction_UpdateAction,
        PaneSelectAction_ActionTypeSelected,
        PaneSelectAction_InitiateSaveAction,
        PaneSelectAction_ActionAddRequest,
        PaneSelectAction_ActivityTypeSelected
    }

    export class ActionTypeSelectedEventArgs {
        public action: interfaces.IActionDTO

        constructor(action: interfaces.IActionDTO) {
            // Clone Action to prevent any issues due to possible mutation of source object
            this.action = angular.extend({}, action);
        }
    }

    export class ActivityTypeSelectedEventArgs {
        public activityTemplate: interfaces.IActivityTemplateVM

        constructor(activityTemplate: interfaces.IActivityTemplateVM) {
            // Clone Action to prevent any issues due to possible mutation of source object
            this.activityTemplate = angular.extend({}, activityTemplate);
        }
    }


    export class ActionUpdatedEventArgs extends ActionEventArgsBase {
        public isTempId: boolean;
        public actionName: string;

        constructor(criteriaId: number, actionId: number, isTempId: boolean, actionName: string) {
            super(criteriaId, actionId);
            this.isTempId = isTempId;
            this.actionName = actionName;
        }
    }

    export class RenderEventArgs {
        public processNodeTemplateId: number;
        public id: number;
        public isTempId: boolean;
        public actionListId: number;

        constructor(
            processNodeTemplateId: number,
            id: number,
            isTemp: boolean,
            actionListId: number) {

            this.processNodeTemplateId = processNodeTemplateId;
            this.id = id;
            this.isTempId = isTemp;
            this.actionListId = actionListId;
        }
    }

    export class UpdateActionEventArgs extends ActionEventArgsBase {
        public isTempId: boolean;

        constructor(criteriaId: number, actionId: number, isTempId: boolean) {
            super(criteriaId, actionId);
            this.isTempId = isTempId;
        }
    }

    export class ActionRemovedEventArgs {
        public id: number;
        public isTempId: boolean;

        constructor(id: number, isTempId: boolean) {
            this.id = id;
            this.isTempId = isTempId;
        }
    }

    export class ActionAddRequestEventArgs {
        constructor() {
        }
    }


    export interface IPaneSelectActionScope extends ng.IScope {
    }

    //More detail on creating directives in TypeScript: 
    //http://blog.aaronholmes.net/writing-angularjs-directives-as-typescript-classes/
    class PaneSelectAction implements ng.IDirective {
        public link: (scope: IPaneSelectActionScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public controller: ($scope: IPaneSelectActionScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public scope = {
            field: '='
        };

        public restrict = 'E';
        private _$element: ng.IAugmentedJQuery;
        private _$scope: IPaneSelectActionScope;

        constructor(private $modal: any, private ActivityTemplateService: services.IActivityTemplateService) {
            PaneSelectAction.prototype.link = (
                scope: IPaneSelectActionScope,
                element: ng.IAugmentedJQuery,
                attrs: ng.IAttributes) => {

                //Link function goes here
            };

            PaneSelectAction.prototype.controller = (
                $scope: IPaneSelectActionScope,
                $element: ng.IAugmentedJQuery,
                $attrs: ng.IAttributes) => {
                this._$element = $element;
                this._$scope = $scope;

                $scope.$on(MessageType[MessageType.PaneSelectAction_ActionAddRequest], <any>angular.bind(this, this.onActionAddRequest));
            };
        }

        private onActionAddRequest() {
            //we should list available actions to user and let him select one
            this.ActivityTemplateService.getAvailableActivities().$promise.then((categoryList: Array<interfaces.IActivityCategoryDTO>) => {
                //we should open a modal to let user select one of our activities
                this.$modal.open({
                    animation: true,
                    templateUrl: 'AngularTemplate/PaneSelectActionModal',
                    //this is a simple modal controller, so i didn't have an urge to seperate this
                    //but resolve is used to make future seperation easier
                    controller: ['$modalInstance', '$scope', 'activityCategories', ($modalInstance, $modalScope, activityCategories: Array<interfaces.IActivityCategoryDTO>) => {
                        $modalScope.activityCategories = activityCategories;
                        $modalScope.activityTypeSelected = (activityType: interfaces.IActivityTemplateVM) => {
                            $modalInstance.close(activityType);
                        };
                        $modalScope.cancel = () => {
                            $modalInstance.dismiss();
                        };
                    }],
                    resolve: {
                        'activityCategories': () => categoryList
                    },
                    windowClass: 'select-action-modal'
                }).result.then((selectedActivity: interfaces.IActivityTemplateVM) => {
                    //now we should emit an activity type selected event
                    var eventArgs = new ActivityTypeSelectedEventArgs(selectedActivity);
                    this._$scope.$emit(MessageType[MessageType.PaneSelectAction_ActivityTypeSelected], eventArgs);
                });
            });
        }

        //The factory function returns Directive object as per Angular requirements
        public static Factory() {
            var directive = ($modal: any, ActivityTemplateService : services.IActivityTemplateService) => {
                return new PaneSelectAction($modal, ActivityTemplateService);
            };

            directive['$inject'] = ['$modal', 'ActivityTemplateService'];
            return directive;
        }
    }

    app.directive('paneSelectAction', PaneSelectAction.Factory());
}