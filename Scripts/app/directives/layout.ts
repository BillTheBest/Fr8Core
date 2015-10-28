﻿/// <reference path="../_all.ts" />

module dockyard.directives {

    app.directive('layoutAction', (LayoutService: services.LayoutService) => {
        return {
            restrict: 'A',
            link: (scope: any, elem: ng.IAugmentedJQuery) => {
                scope.$watch(() => elem.height(), (newValue) => {
                    scope.action.height = newValue;
                    if (newValue > scope.group.height)
                        scope.group.height = newValue;
                    LayoutService.recalculateTop(scope.actionGroups);
                });
            }
        };
    });

    app.directive('layoutActionGroup', (LayoutService: services.LayoutService) => {
        return {
            restrict: 'A',
            link: (scope: any, elem: ng.IAugmentedJQuery) => {
            }
        };
    });

    // calculates process builder container height depending on the amount of actions
    app.directive('layoutContainer', (LayoutService: services.LayoutService) => {
        return {
            restrict: 'A',
            link: (scope: ng.IScope, elem: ng.IAugmentedJQuery) => {
                scope.$watch(() => {
                    var lastChild = elem.children().last();
                    if (lastChild.length) {
                        return lastChild.position().top + elem.scrollTop() + lastChild.height() + 30;
                    }
                    return 0;
                }, (newValue) => {
                    elem.css('height', newValue);
                });
            }
        };
    });
}