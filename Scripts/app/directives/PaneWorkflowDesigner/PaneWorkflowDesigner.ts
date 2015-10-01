﻿/// <reference path="../../_all.ts" />

module dockyard.directives.paneWorkflowDesigner {
    declare var Core: any;
    declare var ProcessBuilder: any;

    export function PaneWorkflowDesigner(): ng.IDirective {

        var onRender = function (eventArgs: RenderEventArgs, scope: IPaneWorkflowDesignerScope) {
            console.log('PaneWorkflowDesigner::onRender', eventArgs);
        };

        var onActionAdded = function (eventArgs: AddActionEventArgs, scope: IPaneWorkflowDesignerScope) {
            console.log('PaneWorkflowDesigner::onActionAdded', eventArgs);
            
            var actionObj = <any>eventArgs.action;

            scope.widget.addAction(eventArgs.criteriaId, eventArgs.action, eventArgs.actionListType);

            scope.$emit(
                MessageType[MessageType.PaneWorkflowDesigner_ActionSelected],
                new ActionSelectedEventArgs(eventArgs.criteriaId, eventArgs.action.id, eventArgs.actionListType, 0)
                );
        };


        var onActionRemoved = function (eventArgs: ActionRemovedEventArgs, scope: IPaneWorkflowDesignerScope) {
            console.log('PaneWorkflowDesigner::onActionRemove', eventArgs);
            scope.widget.removeAction(eventArgs.id, eventArgs.isTempId);
        };

        var onProcessNodeTemplateTempIdReplaced = function (eventArgs: ReplaceTempIdForProcessNodeTemplateEventArgs, scope: IPaneWorkflowDesignerScope) {
            scope.widget.replaceCriteriaTempId(eventArgs.tempId, eventArgs.id);
        };

        var onProcessNodeTemplateRenamed = function (eventArgs: UpdateProcessNodeTemplateNameEventArgs, scope: IPaneWorkflowDesignerScope) {
            scope.widget.renameCriteria(eventArgs.id, eventArgs.text);
        };

        var onActionTempIdReplaced = function (eventArgs: ActionTempIdReplacedEventArgs, scope: IPaneWorkflowDesignerScope) {
            scope.widget.replaceActionTempId(eventArgs.tempId, eventArgs.id);
        };

        var onActionRenamed = function (eventArgs: ActionNameUpdatedEventArgs, scope: IPaneWorkflowDesignerScope) {
            scope.widget.renameAction(eventArgs.id, eventArgs.name);
        };

        var onUpdateActivityTemplateIdForAction = function (eventArgs: UpdateActivityTemplateIdEventArgs, scope: IPaneWorkflowDesignerScope) {
            scope.widget.updateActivityTemplateId(eventArgs.id, eventArgs.activityTemplateId);
        };

        return {
            restrict: 'E',
            template: '<div style="overflow: auto;"></div>',
            scope: {},
            link: (scope: IPaneWorkflowDesignerScope, element: JQuery, attrs: any): void => {
                var factory = new ProcessBuilder.FabricJsFactory();
                var widget = Core.create(ProcessBuilder.Widget,
                    element.children()[0], factory, attrs.width, attrs.height);

                widget.on('addActionNode:click', function (e, criteriaId, actionType) {
                    scope.$apply(function () {
                        scope.$emit(
                            MessageType[MessageType.PaneWorkflowDesigner_ActionAdding],
                            new ActionAddingEventArgs(criteriaId, <model.ActionListType>actionType)
                        );
                    });
                });

                widget.on('actionNode:click', function (e, criteriaId, actionId, actionType, activityTemplateId) {
                    scope.$apply(function () {
                        scope.$emit(
                            MessageType[MessageType.PaneWorkflowDesigner_ActionSelected],
                            new ActionSelectedEventArgs(criteriaId, actionId, <model.ActionListType>actionType, activityTemplateId)
                        );
                    });
                });

                scope.widget = widget;

                // Event handlers.
                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_Render],
                    (event: ng.IAngularEvent, eventArgs: RenderEventArgs) => onRender(eventArgs, scope));

                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_AddAction],
                    (event: ng.IAngularEvent, eventArgs: AddActionEventArgs) => onActionAdded(eventArgs, scope));

                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_ActionRemoved],
                    (event: ng.IAngularEvent, eventArgs: ActionRemovedEventArgs) => onActionRemoved(eventArgs, scope));

                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_ReplaceTempIdForProcessNodeTemplate],
                    (event: ng.IAngularEvent, eventArgs: ReplaceTempIdForProcessNodeTemplateEventArgs) => onProcessNodeTemplateTempIdReplaced(eventArgs, scope));

                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_ActionTempIdReplaced],
                    (event: ng.IAngularEvent, eventArgs: ActionTempIdReplacedEventArgs) => onActionTempIdReplaced(eventArgs, scope));

                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_UpdateProcessNodeTemplateName],
                    (event: ng.IAngularEvent, eventArgs: UpdateProcessNodeTemplateNameEventArgs) => onProcessNodeTemplateRenamed(eventArgs, scope));

                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_ActionNameUpdated],
                    (event: ng.IAngularEvent, eventArgs: ActionNameUpdatedEventArgs) => onActionRenamed(eventArgs, scope));

                scope.$on(MessageType[MessageType.PaneWorkflowDesigner_UpdateActivityTemplateId],
                    (event: ng.IAngularEvent, eventArgs: UpdateActivityTemplateIdEventArgs) => onUpdateActivityTemplateIdForAction(eventArgs, scope));


            }
        };
    }
}

app.directive('paneWorkflowDesigner', dockyard.directives.paneWorkflowDesigner.PaneWorkflowDesigner);
