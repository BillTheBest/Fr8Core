﻿module dockyard.tests.utils.fixtures {

    export class CrateHelper {

        public static emptyStorage: model.CrateStorage = {
            crates: [],
            crateDTO: null
        }

        public static sampleStorage: model.CrateStorage = {
            crates: [
                <model.Crate> {
                    "id": "435532ca-b294-44c5-be09-f72338b8dd3e",
                    "label": "Configuration_Controls",
                    "contents": {
                        "Controls": [
                            {
                                "groupName": "SMSNumber_Group",
                                "radios": [
                                    {
                                        "selected": true,
                                        "name": "SMSNumberOption",
                                        "value": "SMS Number",
                                        "controls": [
                                            {
                                                "name": "SMS_Number",
                                                "required": true,
                                                "value": null,
                                                "label": null,
                                                "type": "TextBox",
                                                "selected": false,
                                                "events": null,
                                                "source": null
                                            }
                                        ]
                                    },
                                    {
                                        "selected": true,
                                        "name": "SMSNumberOption",
                                        "value": "A value from Upstream Crate",
                                        "controls": [
                                            {
                                                "listItems": [

                                                ],
                                                "name": "upstream_crate",
                                                "required": false,
                                                "value": null,
                                                "label": null,
                                                "type": "DropDownList",
                                                "selected": false,
                                                "events": [
                                                    {
                                                        "name": "onChange",
                                                        "handler": "requestConfig"
                                                    }
                                                ],
                                                "source": {
                                                    "manifestType": "Standard Design-Time Fields",
                                                    "label": "Available Fields"
                                                }
                                            }
                                        ]
                                    }
                                ],
                                "name": null,
                                "required": false,
                                "value": null,
                                "label": "For the SMS Number use:",
                                "type": "RadioButtonGroup",
                                "selected": false,
                                "events": null,
                                "source": null
                            },
                            {
                                "name": "SMS_Body",
                                "required": true,
                                "value": null,
                                "label": "SMS Body",
                                "type": "TextBox",
                                "selected": false,
                                "events": null,
                                "source": null
                            }
                        ]
                    },
                    "parentCrateId": null,
                    "manifestType": "Standard UI Controls",
                    "manifestId": "6",
                    "manufacturer": null,
                    "createTime": "0001-01-01T00:00:00"
                },
                <model.Crate> {
                    "id": "97f09d75-6810-45c3-9bdb-56912e4606c8",
                    "label": "Available Fields",
                    "contents": {
                        "Fields": [
                            {
                                "key": "+15005550006",
                                "value": "+15005550006"
                            }
                        ]
                    },
                    "parentCrateId": null,
                    "manifestType": "Standard Design-Time Fields",
                    "manifestId": "3",
                    "manufacturer": null,
                    "createTime": "0001-01-01T00:00:00"
                },
                <model.Crate> {
                    "id": "97f09d75-6810-45c3-9bdb-56912e4606c8",
                    "label": "Available Fields1",
                    "contents": {
                        "Fields": [
                            {
                                "key": "Test",
                                "value": "test"
                            }
                        ]
                    },
                    "parentCrateId": null,
                    "manifestType": "Standard Design-Time Fields1",
                    "manifestId": "5",
                    "manufacturer": null,
                    "createTime": "0001-01-01T00:00:00"
                },
                <model.Crate> {
                    id: "97f09d75-6810-45c3-9bdb-56912e4606c8",
                    label: "Available Fields2",
                    contents: {
                        "Fields": [
                            {
                                "key": "Test1",
                                "value": "test1"
                            }
                        ]
                    },
                    parentCrateId: null,
                    manifestType: "Standard Design-Time Fields2",
                    manifestId: "7",
                    manufacturer: null,
                    createTime: "0001-01-01T00:00:00"
                }
            ],
            crateDTO: null
        }

        public static duplicateStorage: model.CrateStorage = {
            crates: [
                <model.Crate> {
                    "id": "97f09d75-6810-45c3-9bdb-56912e4606c8",
                    "label": "Available Fields",
                    "contents": {
                        "Fields": [
                            {
                                "key": "+15005550006",
                                "value": "+15005550006"
                            }
                        ]
                    },
                    "parentCrateId": null,
                    "manifestType": "Standard Design-Time Fields",
                    "manifestId": "3",
                    "manufacturer": null,
                    "createTime": "0001-01-01T00:00:00"
                },
                <model.Crate> {
                    "id": "97f09d75-6810-45c3-9bdb-56912e4606c8",
                    "label": "Available Fields",
                    "contents": {
                        "Fields": [
                            {
                                "key": "+15005550006",
                                "value": "+15005550006"
                            }
                        ]
                    },
                    "parentCrateId": null,
                    "manifestType": "Standard Design-Time Fields",
                    "manifestId": "3",
                    "manufacturer": null,
                    "createTime": "0001-01-01T00:00:00"
                }
            ],
            crateDTO: null
        }

        public static controlsList: model.ControlsList = <any>{
            fields: [
                {
                    "name": "SMS_Number",
                    "required": true,
                    "value": null,
                    "label": null,
                    "type": "TextBox",
                    "selected": false,
                    "events": null,
                    "source": null,
                    fieldLabel: null
                },
                {
                    "name": "SMS_Body",
                    "required": true,
                    "value": null,
                    "label": "SMS Body",
                    "type": "TextBox",
                    "selected": false,
                    "events": null,
                    "source": null,
                    fieldLabel: null
                }
            ]
        }

        public static fields = [
            <model.ControlDefinitionDTO> {
                "groupName": "SMSNumber_Group",
                "radios": [
                    {
                        "selected": true,
                        "name": "SMSNumberOption",
                        "value": "SMS Number",
                        "controls": [
                            {
                                "name": "SMS_Number",
                                "required": true,
                                "value": null,
                                "label": null,
                                "type": "TextBox",
                                "selected": false,
                                "events": null,
                                "source": null
                            }
                        ]
                    },
                    {
                        "selected": true,
                        "name": "SMSNumberOption",
                        "value": "A value from Upstream Crate",
                        "controls": [
                            {
                                "listItems": [],
                                "name": "upstream_crate",
                                "required": false,
                                "value": null,
                                "label": null,
                                "type": "DropDownList",
                                "selected": false,
                                "events": [
                                    {
                                        "name": "onChange",
                                        "handler": "requestConfig"
                                    }
                                ],
                                "source": {
                                    "manifestType": "Standard Design-Time Fields",
                                    "label": "Available Fields"
                                }
                            },
                            {
                                "listItems": [],
                                "name": "upstream_crate",
                                "required": false,
                                "value": null,
                                "label": null,
                                "type": "TextSource",
                                "selected": false,
                                "events": [],
                                "source": {
                                    "manifestType": "Standard Design-Time Fields2",
                                    "label": "Available Fields2"
                                },
                                "initialLabel": "Initial label",
                                "valueSource": "Value source"
                            }

                        ]
                    }
                ],
                "name": null,
                "required": false,
                "value": null,
                "label": "For the SMS Number use:",
                "type": "RadioButtonGroup",
                "selected": false,
                "events": null,
                "source": null,
                fieldLabel: null
            },
            <model.ControlDefinitionDTO> {
                "name": "SMS_Body",
                "required": true,
                "value": null,
                "label": "SMS Body",
                "type": "TextBox",
                "selected": false,
                "events": null,
                "source": null,
                fieldLabel: null
            },
            <model.ControlDefinitionDTO> {
                "listItems": [],
                "name": "upstream_crate1",
                "required": false,
                "value": null,
                "label": null,
                "type": "DropDownList",
                "selected": false,
                "events": [],
                "source": {
                    "manifestType": "Standard Design-Time Fields1",
                    "label": "Available Fields1"
                },
                fieldLabel: null
            }
        ]

    }

} 