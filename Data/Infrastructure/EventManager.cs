﻿//We rename .NET style "events" to "alerts" to avoid confusion with our business logic Alert concepts

using System;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using System.Data.Entity.Infrastructure;

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

        public delegate void IncidentTerminalConfigurePOSTFailureHandler(string terminalUrl, string curActionDTO, string errorMessage, string objectId);
        public static event IncidentTerminalConfigurePOSTFailureHandler IncidentTerminalConfigureFailed;

        public delegate void IncidentTerminalRunPOSTFailureHandler(string terminalUrl, string curActionDTO, string errorMessage, string objectId);
        public static event IncidentTerminalRunPOSTFailureHandler IncidentTerminalRunFailed;

        public delegate void IncidentTerminalInternalFailureHandler(string terminalUrl, string curActionDTO, Exception e, string objectId);
        public static event IncidentTerminalInternalFailureHandler IncidentTerminalInternalFailureOccurred;

        public delegate void IncidentTerminalActionActivationPOSTFailureHandler(string terminalUrl, string curActionDTO, string objectId);
        public static event IncidentTerminalActionActivationPOSTFailureHandler IncidentTerminalActionActivationFailed;

        public delegate void TerminalActionActivatedHandler(ActionDO action);
        public static event TerminalActionActivatedHandler TerminalActionActivated;


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

        public delegate void IncidentOAuthAuthenticationFailedHandler(string requestQueryString, string errorMessage);
        public static event IncidentOAuthAuthenticationFailedHandler IncidentOAuthAuthenticationFailed;

        public delegate void UserRegistrationHandler(Fr8AccountDO curUser);
        public static event UserRegistrationHandler AlertUserRegistration;

        public delegate void Fr8AccountTerminalRegistrationHandler(TerminalDO terminalDO);
        public static event Fr8AccountTerminalRegistrationHandler AlertFr8AccountTerminalRegistration;

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

        public delegate void TerminalIncidentHandler(LoggingDataCm incidentItem);
        public static event TerminalIncidentHandler TerminalIncidentReported;

        public delegate void EventDocuSignNotificationReceivedHandler();
        public static event EventDocuSignNotificationReceivedHandler EventDocuSignNotificationReceived;

        public delegate void EventContainerLaunchedHandler(ContainerDO launchedContainer);
        public static event EventContainerLaunchedHandler EventContainerLaunched;

        public delegate void EventContainerCreatedHandler(ContainerDO containerDO);
        public static event EventContainerCreatedHandler EventContainerCreated;

        public delegate void EventContainerSentHandler(ContainerDO containerDO, ActionDO actionDO);
        public static event EventContainerSentHandler EventContainerSent;

        public delegate void EventContainerReceivedHandler(ContainerDO containerDO, ActionDO actionDO);
        public static event EventContainerReceivedHandler EventContainerReceived;

        public delegate void EventContainerStateChangedHandler(DbPropertyValues currentValues);
        public static event EventContainerStateChangedHandler EventContainerStateChanged;

        public delegate void EventProcessNodeCreatedHandler(ProcessNodeDO processNode);
        public static event EventProcessNodeCreatedHandler EventProcessNodeCreated;

        public delegate void EventCriteriaEvaluationStartedHandler(Guid processId);
        public static event EventCriteriaEvaluationStartedHandler EventCriteriaEvaluationStarted;

        public delegate void EventCriteriaEvaluationFinishedHandler(Guid processId);
        public static event EventCriteriaEvaluationFinishedHandler EventCriteriaEvaluationFinished;

        public delegate void EventActionStartedHandler(ActionDO action);
        public static event EventActionStartedHandler EventActionStarted;

        public delegate void EventActionDispatchedHandler(ActionDO curAction, Guid processId);
        public static event EventActionDispatchedHandler EventActionDispatched;

        public delegate void TerminalEventHandler(LoggingDataCm eventDataCm);
        public static event TerminalEventHandler TerminalEventReported;

        public delegate void ExternalEventReceivedHandler(string curEventPayload);
        public static event ExternalEventReceivedHandler ExternalEventReceived;

        public delegate void IncidentDocuSignFieldMissingHandler(string envelopeId, string fieldName);
        public static event IncidentDocuSignFieldMissingHandler IncidentDocuSignFieldMissing;

        public delegate void IncidentMissingFieldInPayloadHandler(string fieldKey, ActionDO action, string curUserId);
        public static event IncidentMissingFieldInPayloadHandler IncidentMissingFieldInPayload;

        public delegate void UnparseableNotificationReceivedHandler(string curNotificationUrl, string curNotificationPayload);
        public static event UnparseableNotificationReceivedHandler UnparseableNotificationReceived;

        public delegate void EventTwilioSMSSentHandler(string number, string message);
        public static event EventTwilioSMSSentHandler EventTwilioSMSSent;

        public delegate void IncidentTwilioSMSSendFailureHandler(string number, string message, string errorMsg);
        public static event IncidentTwilioSMSSendFailureHandler IncidentTwilioSMSSendFailure;

        public delegate object AuthenticationCompletedEventHandler(string userId, TerminalDO authenticatedTerminal);
        public static event AuthenticationCompletedEventHandler EventAuthenticationCompleted;

        public delegate void KeyVaultFailureHandler(Exception ex);
        public static event KeyVaultFailureHandler KeyVaultFailure;

        #region Method

        public static void KeyVaultFailed(Exception ex)
        {
            var handler = KeyVaultFailure;

            if (handler != null)
            {
                handler.Invoke(ex);
            }
        }

        public static void TerminalConfigureFailed(string terminalUrl, string actionDTO, string errorMessage, string objectId)
        {
            IncidentTerminalConfigurePOSTFailureHandler handler = IncidentTerminalConfigureFailed;
            if (handler != null) handler(terminalUrl, actionDTO, errorMessage, objectId);
        }

        public static void TerminalRunFailed(string terminalUrl, string actionDTO, string errorMessage, string objectId)
        {
            IncidentTerminalRunPOSTFailureHandler handler = IncidentTerminalRunFailed;
            if (handler != null) handler(terminalUrl, actionDTO, errorMessage, objectId);
        }

        public static void TerminalInternalFailureOccurred(string terminalUrl, string actionDTO, Exception e, string objectId)
        {
            IncidentTerminalInternalFailureHandler handler = IncidentTerminalInternalFailureOccurred;
            if (handler != null) handler(terminalUrl, actionDTO, e, objectId);
        }

        public static void TerminalActionActivationFailed(string terminalUrl, string actionDTO, string objectId)
        {
            IncidentTerminalActionActivationPOSTFailureHandler handler = IncidentTerminalActionActivationFailed;
            if (handler != null) handler(terminalUrl, actionDTO, objectId);
        }

        public static void UserNotification(string userid, string message, TimeSpan expiresIn = default(TimeSpan))
        {
            UserNotificationHandler handler = AlertUserNotification;
            if (handler != null) handler(userid, message, expiresIn);
        }

        public static void ReportTerminalIncident(LoggingDataCm incidentItem)
        {
            TerminalIncidentHandler handler = TerminalIncidentReported;
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

        public static void Fr8AccountTerminalRegistration(TerminalDO terminalDO)
        {
            if (AlertFr8AccountTerminalRegistration != null)
                AlertFr8AccountTerminalRegistration(terminalDO);
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

        public static void CriteriaEvaluationStarted(Guid processId)
        {
            var handler = EventCriteriaEvaluationStarted;
            if (handler != null) handler(processId);
        }

        public static void CriteriaEvaluationFinished(Guid processId)
        {
            var handler = EventCriteriaEvaluationFinished;
            if (handler != null) handler(processId);
        }

        public static void ActionStarted(ActionDO action)
        {
            var handler = EventActionStarted;
            if (handler != null) handler(action);
        }

        public static void ActionDispatched(ActionDO curAction, Guid processId)
        {
            var handler = EventActionDispatched;
            if (handler != null) handler(curAction, processId);
        }

        public static void ReportTerminalEvent(LoggingDataCm eventDataCm)
        {
            TerminalEventHandler handler = TerminalEventReported;
            if (handler != null) handler(eventDataCm);
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
        public static void MissingFieldInPayload(string fieldKey, ActionDO action, string userId)
        {
            var handler = IncidentMissingFieldInPayload;
            if (handler != null) handler(fieldKey, action, userId);
        }

        public static void OAuthAuthenticationFailed(string requestQueryString, string errorMessage)
        {
            var handler = IncidentOAuthAuthenticationFailed;
            if (handler != null) handler(requestQueryString, errorMessage);
        }

        public static void ActionActivated(ActionDO action)
        {
            var handler = TerminalActionActivated;
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

        public static void ContainerCreated(ContainerDO containerDO)
        {
            var handler = EventContainerCreated;
            if (handler != null) handler(containerDO);
        }

        public static void ContainerSent(ContainerDO containerDO, ActionDO actionDO)
        {
            var handler = EventContainerSent;
            if (handler != null) handler(containerDO, actionDO);
        }

        public static void ContainerReceived(ContainerDO containerDO, ActionDO actionDO)
        {
            var handler = EventContainerReceived;
            if (handler != null) handler(containerDO, actionDO);
        }
        internal static void ContainerStateChanged(DbPropertyValues currentValues)
        {
            var handler = EventContainerStateChanged;
            if (handler != null) handler(currentValues);
        }

        public static void TerminalAuthenticationCompleted(string userId, TerminalDO authenticatedTerminal)
        {
            var handler = EventAuthenticationCompleted;
            if (handler != null) handler(userId, authenticatedTerminal);
        }


        #endregion
    }

}