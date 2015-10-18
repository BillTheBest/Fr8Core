﻿//We rename .NET style "events" to "alerts" to avoid confusion with our business logic Alert concepts

using System;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;

namespace Data.Infrastructure
{
    //this class serves as both a registry of all of the defined alerts as well as a utility class.
    public static class EventManager
    {


        public delegate void ResponseRecievedHandler(int bookingRequestId, String bookerID, String customerID);
        public static event ResponseRecievedHandler AlertResponseReceived;


        public delegate void TrackablePropertyUpdatedHandler(string name, string contextTable, object id, object status);
        public static event TrackablePropertyUpdatedHandler AlertTrackablePropertyUpdated;

        public delegate void EntityStateChangedHandler(string entityName, object id, string stateName, string stateValue);
        public static event EntityStateChangedHandler AlertEntityStateChanged;

        public delegate void IncidentPluginConfigurePOSTFailureHandler(string pluginUrl, string curActionDTO, string errorMessage);
        public static event IncidentPluginConfigurePOSTFailureHandler IncidentPluginConfigureFailed;

        public delegate void IncidentPluginActionActivationPOSTFailureHandler(string pluginUrl, string curActionDTO);
        public static event IncidentPluginActionActivationPOSTFailureHandler IncidentPluginActionActivationFailed;

        public delegate void PluginActionActivatedHandler(ActionDO action);
        public static event PluginActionActivatedHandler PluginActionActivated;
         

        public delegate void ExplicitCustomerCreatedHandler(string curUserId);
        public static event ExplicitCustomerCreatedHandler AlertExplicitCustomerCreated;
   
        public delegate void CustomerCreatedHandler(Fr8AccountDO user);
        public static event CustomerCreatedHandler AlertCustomerCreated;

        public delegate void EmailReceivedHandler(int emailId, string customerId);
        public static event EmailReceivedHandler AlertEmailReceived;

        public delegate void EventBookedHandler(int eventId, string customerId);
        public static event EventBookedHandler AlertEventBooked;

        public delegate void EmailSentHandler(int emailId, string customerId);
        public static event EmailSentHandler AlertEmailSent;

        public delegate void EmailProcessingHandler(string dateReceived, string errorMessage);
        public static event EmailProcessingHandler AlertEmailProcessingFailure;

        public delegate void UserRegistrationHandler(Fr8AccountDO curUser);
        public static event UserRegistrationHandler AlertUserRegistration;

        public delegate void UserRegistrationErrorHandler(Exception ex);
        public static event UserRegistrationErrorHandler AlertUserRegistrationError;

        public delegate void Error_EmailSendFailureHandler(int emailId, string message);
        public static event Error_EmailSendFailureHandler AlertError_EmailSendFailure;

        //public delegate void ErrorSyncingCalendarHandler(IRemoteCalendarAuthDataDO authData, IRemoteCalendarLinkDO calendarLink = null);
        //public static event ErrorSyncingCalendarHandler AlertErrorSyncingCalendar;

        public delegate void HighPriorityIncidentCreatedHandler(int incidentId);
        public static event HighPriorityIncidentCreatedHandler AlertHighPriorityIncidentCreated;

        public delegate void UserNotificationHandler(string userId, string message, TimeSpan expiresIn = default(TimeSpan));
        public static event UserNotificationHandler AlertUserNotification;

        //public delegate void BookingRequestMergedHandler(int originalBRId, int targetBRId);
        //public static event BookingRequestMergedHandler AlertBookingRequestMerged;

        //EventProcessRequestReceived 
        public delegate void EventProcessRequestReceivedHandler(ContainerDO containerId);
        public static event EventProcessRequestReceivedHandler EventProcessRequestReceived;

        public delegate void OAuthEventHandler(string userId);
        public static event OAuthEventHandler AlertTokenRequestInitiated;
        public static event OAuthEventHandler AlertTokenObtained;
        public static event OAuthEventHandler AlertTokenRevoked;

        public delegate void PluginIncidentHandler(LoggingData incidentItem);
        public static event PluginIncidentHandler PluginIncidentReported;

        public delegate void EventDocuSignNotificationReceivedHandler();
        public static event EventDocuSignNotificationReceivedHandler EventDocuSignNotificationReceived;

        public delegate void EventContainerLaunchedHandler(ContainerDO launchedContainer);
        public static event EventContainerLaunchedHandler EventContainerLaunched;

        public delegate void EventProcessNodeCreatedHandler(ProcessNodeDO processNode);
        public static event EventProcessNodeCreatedHandler EventProcessNodeCreated;

        public delegate void EventCriteriaEvaluationStartedHandler(int processId);
        public static event EventCriteriaEvaluationStartedHandler EventCriteriaEvaluationStarted;

        public delegate void EventCriteriaEvaluationFinishedHandler(int processId);
        public static event EventCriteriaEvaluationFinishedHandler EventCriteriaEvaluationFinished;

        public delegate void EventActionStartedHandler(ActionDO action);
        public static event EventActionStartedHandler EventActionStarted;

        public delegate void EventActionDispatchedHandler(ActionDO curAction, int processId);
        public static event EventActionDispatchedHandler EventActionDispatched;

        public delegate void PluginEventHandler(LoggingData eventData);
        public static event PluginEventHandler PluginEventReported;

        public delegate void ExternalEventReceivedHandler(string curEventPayload);
        public static event ExternalEventReceivedHandler ExternalEventReceived;

        public delegate void IncidentDocuSignFieldMissingHandler(string envelopeId, string fieldName);
        public static event IncidentDocuSignFieldMissingHandler IncidentDocuSignFieldMissing;

        public delegate void UnparseableNotificationReceivedHandler(string curNotificationUrl, string curNotificationPayload);
        public static event UnparseableNotificationReceivedHandler UnparseableNotificationReceived;

        public delegate void EventTwilioSMSSentHandler(string number, string message);
        public static event EventTwilioSMSSentHandler EventTwilioSMSSent;

        public delegate void IncidentTwilioSMSSendFailureHandler(string number, string message, string errorMsg);
        public static event IncidentTwilioSMSSendFailureHandler IncidentTwilioSMSSendFailure;
        #region Method


        public static void PluginConfigureFailed(string pluginUrl, string actionDTO, string errorMessage)
        {
            IncidentPluginConfigurePOSTFailureHandler handler = IncidentPluginConfigureFailed;
            if (handler != null) handler(pluginUrl, actionDTO, errorMessage);
        }

        public static void PluginActionActivationFailed(string pluginUrl, string actionDTO)
        {
            IncidentPluginActionActivationPOSTFailureHandler handler = IncidentPluginActionActivationFailed;
            if (handler != null) handler(pluginUrl, actionDTO);
        }

        public static void UserNotification(string userid, string message, TimeSpan expiresIn = default(TimeSpan))
        {
            UserNotificationHandler handler = AlertUserNotification;
            if (handler != null) handler(userid, message, expiresIn);
        }

        public static void ReportPluginIncident(LoggingData incidentItem)
        {
            PluginIncidentHandler handler = PluginIncidentReported;
            if (handler != null) handler(incidentItem);
        }

        //public static void AttendeeUnresponsivenessThresholdReached(int expectedResponseId)
        //{
        //    AttendeeUnresponsivenessThresholdReachedHandler handler = AlertAttendeeUnresponsivenessThresholdReached;
        //    if (handler != null) handler(expectedResponseId);
        //}

        public static void ResponseReceived(int bookingRequestId, String bookerID, String customerID)
        {
            if (AlertResponseReceived != null)
                AlertResponseReceived(bookingRequestId, bookerID, customerID);
        }

        public static void EntityStateChanged(string entityName, object id, string stateName, string stateValue)
        {
            if (AlertEntityStateChanged != null)
                AlertEntityStateChanged(entityName, id, stateName, stateValue);
        }

        public static void TrackablePropertyUpdated(string entityName, string propertyName, object id, object value)
        {
            if (AlertTrackablePropertyUpdated != null)
                AlertTrackablePropertyUpdated(entityName, propertyName, id, value);
        }

        //public static void ConversationMemberAdded(int bookingRequestID)
        //{
        //    if (AlertConversationMemberAdded != null)
        //        AlertConversationMemberAdded(bookingRequestID);
        //}

        //public static void ConversationMatched(int emailID, string subject, int bookingRequestID)
        //{
        //    if (AlertConversationMatched != null)
        //        AlertConversationMatched(emailID, subject, bookingRequestID);
        //}

        /// <summary>
        /// Publish Customer Created event
        /// </summary>
        public static void ExplicitCustomerCreated(string curUserId)
        {
            if (AlertExplicitCustomerCreated != null)
                AlertExplicitCustomerCreated(curUserId);
        }



        public static void CustomerCreated(Fr8AccountDO user)
        {
            if (AlertCustomerCreated != null)
                AlertCustomerCreated(user);
        }

        //public static void BookingRequestCreated(int bookingRequestId)
        //{
        //    if (AlertBookingRequestCreated != null)
        //        AlertBookingRequestCreated(bookingRequestId);
        //}

        public static void EmailReceived(int emailId, string customerId)
        {
            if (AlertEmailReceived != null)
                AlertEmailReceived(emailId, customerId);
        }
        public static void EventBooked(int eventId, string customerId)
        {
            if (AlertEventBooked != null)
                AlertEventBooked(eventId, customerId);
        }
        public static void EmailSent(int emailId, string customerId)
        {
            if (AlertEmailSent != null)
                AlertEmailSent(emailId, customerId);
        }

        public static void EmailProcessingFailure(string dateReceived, string errorMessage)
        {
            if (AlertEmailProcessingFailure != null)
                AlertEmailProcessingFailure(dateReceived, errorMessage);
        }

        //public static void BookingRequestProcessingTimeout(int bookingRequestId, string bookerId)
        //{
        //    if (AlertBookingRequestProcessingTimeout != null)
        //        AlertBookingRequestProcessingTimeout(bookingRequestId, bookerId);
        //}

        //public static void BookingRequestReserved(int bookingRequestId, string bookerId)
        //{
        //    BookingRequestReservedHandler handler = AlertBookingRequestReserved;
        //    if (handler != null) handler(bookingRequestId, bookerId);
        //}

        //public static void BookingRequestReservationTimeout(int bookingRequestId, string bookerId)
        //{
        //    BookingRequestReservationTimeoutHandler handler = AlertBookingRequestReservationTimeout;
        //    if (handler != null) handler(bookingRequestId, bookerId);
        //}

        //public static void StaleBookingRequestsDetected(BookingRequestDO[] oldbookingrequests)
        //{
        //    StaleBookingRequestsDetectedHandler handler = AlertStaleBookingRequestsDetected;
        //    if (handler != null) handler(oldbookingrequests);
        //}

        public static void UserRegistration(Fr8AccountDO curUser)
        {
            if (AlertUserRegistration != null)
                AlertUserRegistration(curUser);
        }

        public static void UserRegistrationError(Exception ex)
        {
            UserRegistrationErrorHandler handler = AlertUserRegistrationError;
            if (handler != null) handler(ex);
        }

        //public static void BookingRequestCheckedOut(int bookingRequestId, string bookerId)
        //{
        //    if (AlertBookingRequestCheckedOut != null)
        //        AlertBookingRequestCheckedOut(bookingRequestId, bookerId);
        //}

        //public static void BookingRequestMarkedProcessed(int bookingRequestId, string bookerId)
        //{
        //    if (AlertBookingRequestMarkedProcessed != null)
        //        AlertBookingRequestMarkedProcessed(bookingRequestId, bookerId);
        //}

        //public static void BookingRequestBookerChange(int bookingRequestId, string bookerId)
        //{
        //    if (AlertBookingRequestOwnershipChange != null)
        //        AlertBookingRequestOwnershipChange(bookingRequestId, bookerId);
        //}

        public static void Error_EmailSendFailure(int emailId, string message)
        {
            if (AlertError_EmailSendFailure != null)
                AlertError_EmailSendFailure(emailId, message);
        }

        //public static void ErrorSyncingCalendar(IRemoteCalendarAuthDataDO authData, IRemoteCalendarLinkDO calendarLink = null)
        //{
        //    var handler = AlertErrorSyncingCalendar;
        //    if (handler != null)
        //        handler(authData, calendarLink);
        //}

        //public static void BookingRequestNeedsProcessing(int bookingRequestId)
        //{
        //    var handler = AlertBookingRequestNeedsProcessing;
        //    if (handler != null)
        //        handler(bookingRequestId);
        //}

        public static void HighPriorityIncidentCreated(int incidentId)
        {
            HighPriorityIncidentCreatedHandler handler = AlertHighPriorityIncidentCreated;
            if (handler != null) handler(incidentId);
        }

        //public static void BookingRequestMerged(int originalBRId, int targetBRId)
        //{
        //    BookingRequestMergedHandler handler = AlertBookingRequestMerged;
        //    if (handler != null) handler(originalBRId, targetBRId);
        //}

        public static void TokenRequestInitiated(string userId)
        {
            var handler = AlertTokenRequestInitiated;
            if (handler != null) handler(userId);
        }

        public static void TokenObtained(string userId)
        {
            var handler = AlertTokenObtained;
            if (handler != null) handler(userId);
        }

        public static void TokenRevoked(string userId)
        {
            var handler = AlertTokenRevoked;
            if (handler != null) handler(userId);
        }

        public static void DocuSignNotificationReceived()
        {
            var handler = EventDocuSignNotificationReceived;
            if (handler != null) handler();
        }

        public static void ContainerLaunched(ContainerDO launchedContainer)
        {
            var handler = EventContainerLaunched;
            if (handler != null) handler(launchedContainer);
        }

        public static void ProcessNodeCreated(ProcessNodeDO processNode)
        {
            var handler = EventProcessNodeCreated;
            if (handler != null) handler(processNode);
        }

        public static void CriteriaEvaluationStarted(int processId)
        {
            var handler = EventCriteriaEvaluationStarted;
            if (handler != null) handler(processId);
        }

        public static void CriteriaEvaluationFinished(int processId)
        {
            var handler = EventCriteriaEvaluationFinished;
            if (handler != null) handler(processId);
        }

        public static void ActionStarted(ActionDO action)
        {
            var handler = EventActionStarted;
            if (handler != null) handler(action);
        }

        public static void ActionDispatched(ActionDO curAction, int processId)
        {
            var handler = EventActionDispatched;
            if (handler != null) handler(curAction, processId);
        }

        public static void ReportPluginEvent(LoggingData eventData)
        {
            PluginEventHandler handler = PluginEventReported;
            if (handler != null) handler(eventData);
        }

        public static void ReportExternalEventReceived(string curEventPayload)
        {
            ExternalEventReceivedHandler handler = ExternalEventReceived;
            if (handler != null) handler(curEventPayload);
        }

        public static void ReportUnparseableNotification(string curNotificationUrl, string curNotificationPayload)
        {
            UnparseableNotificationReceivedHandler handler = UnparseableNotificationReceived;
            if (handler != null) handler(curNotificationUrl, curNotificationPayload);
        }

        public static void DocuSignFieldMissing(string envelopeId, string fieldName)
        {
            var handler = IncidentDocuSignFieldMissing;
            if (handler != null) handler(envelopeId, fieldName);
        }
        public static void ActionActivated(ActionDO action)
        {
            var handler = PluginActionActivated;
            if (handler != null) handler(action);
        }

        public static void ProcessRequestReceived(ContainerDO containerDO)
        {
            var handler = EventProcessRequestReceived;
            if (handler != null) handler(containerDO);
        }

        public static void TwilioSMSSent(string number, string message)
        {
            var handler = EventTwilioSMSSent;
            if (handler != null) handler(number, message);
        }

        public static void TwilioSMSSendFailure(string number, string message, string errorMsg)
        {
            var handler = IncidentTwilioSMSSendFailure;
            if (handler != null) handler(number, message, errorMsg);
        }
        #endregion

        
    }

}