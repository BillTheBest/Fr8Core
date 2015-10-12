﻿/// <reference path="../_all.ts" />

module dockyard.directives {
    'use strict';

    export function GeneralSearchWidget(): ng.IDirective {

        var tryFirstFieldKey = function (array: Array<model.Field>): string {
            if (array) {


                return array[0].key;
            }

            return null;
        };

        return {
            restrict: 'E',
            templateUrl: '/AngularTemplate/GeneralSearchWidget',
            scope: {
                fields: '=',
                operators: '=',
                defaultOperator: '=',
                rows: '=',
                currentAction: '=',
                isDisabled: '='
            },

            controller: ($scope: interfaces.IQueryBuilderWidgetScope): void => {


                $scope.addRow = function () {                 
                    $scope.isDisabled = false;
                    if ($scope.isDisabled)
                        return;
                    var condition = new model.Condition(
                        null,
                        $scope.defaultOperator,
                        null
                        );


                    condition.validate();
                    $scope.rows.push(condition);
                };

                $scope.removeRow = function (index) {
                    if ($scope.isDisabled)
                        return;
                    $scope.rows.splice(index, 1);
                };

                $scope.valueChanged = function (row) {
                    row.valueError = !row.value;
                };

                $scope.isActionValid = function (action: interfaces.IActionVM) {
                    return model.ActionDTO.isActionValid(action);
                }


               


            }
        };
    }
}

app.directive('generalSearchWidget', dockyard.directives.GeneralSearchWidget);
 