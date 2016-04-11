﻿namespace Data.Constants
{
    public enum ActivityResponse
    {
        Null = 0,
        Success,
        Error,
        RequestTerminate,
        RequestSuspend,
        SkipChildren,
        ReprocessChildren,
        ExecuteClientActivity,
        ShowDocumentation,
        JumpToActivity,
        JumpToSubplan,
        LaunchAdditionalPlan,

        //new op codes
        Jump,
        Call,
        Break,
    }

    public enum PlanType
    {
        Ongoing,
        RunOnce
    }
}
