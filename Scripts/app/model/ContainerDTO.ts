﻿module dockyard.model {
    export class ContainerDTO {
        id: string;
        name: string;
        planId: number;
        state: number;
        currentPlanNodeId: string;
        nextRouteNodeId: string;
        lastUpdated: string;
        createDate: string;
        currentActivityResponse: ActivityResponse;
        currentPlanType: PlanType;
        currentClientActivityName: string;
        error: any;
    }

    export enum State {
        Unstarted = 1,
        Executing = 2,
        WaitingForTerminal = 3,
        Completed = 4,
        Failed = 5
    }

    export enum ActivityResponse {
        Null = 0,
        Success = 1,
        Error = 2,
        RequestTerminate = 3,
        RequestSuspend = 4,
        SkipChildren = 5,
        ReprocessChildren = 6,
        ExecuteClientAction = 7
    }

    export enum PlanType {
        OnGoing = 0,
        RunOnce = 1
    }
}