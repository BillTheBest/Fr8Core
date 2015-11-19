﻿module dockyard.tests.utils.fixtures {

    export class ProcessBuilder {
        public static newProcessTemplate = <interfaces.IRouteVM> {
            id: '89EBF277-0CC4-4D6D-856B-52457F10C686',
            name: "MockProcessTemplate",
            description: "MockProcessTemplate",
            routeState: 1,
            subscribedDocuSignTemplates: [],
            externalEventSubscription: [],
            startingSubrouteId: 1
        };

        public static processBuilderState = new model.ProcessBuilderState();

        public static updatedProcessTemplate = <interfaces.IRouteVM> {
            'name': 'Updated',
            'description': 'Description',
            'routeState': 1,
            'subscribedDocuSignTemplates': ['58521204-58af-4e65-8a77-4f4b51fef626']
        }

        public static fullProcessTemplate = <interfaces.IRouteVM> {
            'name': 'Updated',
            'description': 'Description',
            'routeState': 1,
            'subscribedDocuSignTemplates': ['58521204-58af-4e65-8a77-4f4b51fef626'],
            subroutes: [
                <model.SubrouteDTO>{
                    id: '89EBF277-0CC4-4D6D-856B-52457F10C686',
                    isTempId: false,
                    name: 'Processnode Template 1',
                    actions: [
                        <model.ActionDTO> {
                            id: '89EBF277-0CC4-4D6D-856B-52457F10C686',
                            name: 'Action 1',
                            activityTemplateId: 1,
                            activityTemplate: {
                                id: 1
                            },
                            parentRouteNodeId: '89EBF277-0CC4-4D6D-856B-52457F10C686'
                        },
                        <model.ActionDTO>{
                            id: '82B62831-687F-4BC8-AB64-B421985D5CF3',
                            name: 'Action 2',
                            activityTemplateId: 1,
                            activityTemplate: {
                                id: 1
                            },
                            parentRouteNodeId: '89EBF277-0CC4-4D6D-856B-52457F10C686'
                        }
                    ]
                }]
        }
    }
}
