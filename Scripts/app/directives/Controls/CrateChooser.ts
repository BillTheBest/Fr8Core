﻿/// <reference path="../../_all.ts" />
module dockyard.directives.crateChooser {
    'use strict';

    export interface ICrateChooserScope extends ng.IScope {
        field: model.CrateChooser;
        change: () => (field: model.ControlDefinitionDTO) => void;
        selectCrate: () => void;
        onChange: any;
        currentAction: model.ActivityDTO;
    }


    export function CrateChooser(): ng.IDirective {
        var controller = ['$scope', 'CrateHelper', '$modal', ($scope: ICrateChooserScope, crateHelper: services.CrateHelper, $modal: any) => {

            var populateListItems = (crateDetails: model.CrateDetails) => {
                var ddList = Array<model.ControlDefinitionDTO>();
                ddList.push(crateDetails.manifestType);
                ddList.push(crateDetails.label);
                crateHelper.populateListItemsFromDataSource(ddList, $scope.currentAction.crateStorage);
            };

            var onCratesSelected = (selectedCrates: Array<model.CrateDescriptionDTO>) => {
                //hmm we don't need to do anything here
            };

            $scope.onChange = () => {
                if ($scope.change != null && angular.isFunction($scope.change)) {
                    $scope.change()($scope.field);
                }
            };

            $scope.selectCrate = () => {
                var modalInstance = $modal.open({
                    animation: true,
                    templateUrl: 'TextTemplate-CrateChooserSelectionModal',
                    controller: 'CrateChooser__CrateSelectorModalController',
                    size: 'm',
                    resolve: {
                        'crateDescriptions': () => $scope.field.crateDescriptions,
                        'singleSelection': () => $scope.field.singleManifestOnly
                    }
                });

                modalInstance.result.then(onCratesSelected);
            };
        }];

        return {
            restrict: 'E',
            templateUrl: '/AngularTemplate/CrateChooser',
            controller: controller,
            scope: {
                field: '=',
                change: '&',
                currentAction: '='
            }
        };
    }

    app.directive('crateChooser', CrateChooser);


    app.controller('CrateChooser__CrateSelectorModalController', ['$scope', '$modalInstance', 'crateDescriptions', 'singleSelection', ($scope: any, $modalInstance: any, crateDescriptions: Array<model.CrateDescriptionDTO>, singleSelection: boolean): void => {

        $scope.tpl = '<crate-node single-selection="' + (singleSelection === true ? 'true' : 'false')+'"></crate-node>';

        var categories = [];
        var findCategory = (crateDescription: model.CrateDescriptionDTO) => {
            for (var i = 0; i < categories.length; i++) {
                if (crateDescription.manifestId === categories[i].manifestId) {
                    return categories[i];
                }
            }
            return null;
        };


        for (var i = 0; i < crateDescriptions.length; i++) {
            var category = findCategory(crateDescriptions[i]);
            if (category == null) {
                category = { label: crateDescriptions[i].manifestType, isCategory: true, children: [] };
                categories.push(category);
            }

            category.children.push(crateDescriptions[i]);
        }

        $scope.crateDescriptions = categories;

        $scope.ok = () => {
            var selections = [];
            for (var i = 0; i < crateDescriptions.length; i++) {
                if (crateDescriptions[i].selected) {
                    selections.push(crateDescriptions[i]);
                }
            }
            $modalInstance.close(selections);
        };

        $scope.cancel = () => {
            $modalInstance.dismiss();
        };

    }]);


    app.directive('crateNode', ['ivhTreeviewMgr', (ivhTreeviewMgr) => {
        return {
            restrict: 'E',
            templateUrl: 'TextTemplate-CrateNode',
            link: (scope, element, attrs) => {
                var singleSelection: boolean = attrs.singleSelection === 'true';
                element.on('click', () => {
                    if (scope.node.isCategory) {
                        return;
                    }
                    var currentState = scope.node.selected;
                    if (singleSelection) {
                        //deselect previous selections first
                        ivhTreeviewMgr.deselectAll(scope.trvw.root());
                    }

                    ivhTreeviewMgr.select(scope.trvw.root(), scope.node, !currentState);
                    scope.$apply();
                });
            }
        };
    }]);
}