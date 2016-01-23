﻿using System;
using Data.Entities;
using Microsoft.SqlServer.Server;
using StructureMap;
using Data.Exceptions;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Hub.Services;
using Utilities;
using Utilities.Logging;

namespace Hub.Managers
{
    public class IncidentReporter
    {
        private readonly EventReporter _eventReporter;

        public IncidentReporter(EventReporter eventReporter)
        {
            _eventReporter = eventReporter;
        }
        public void SubscribeToAlerts()
        {
            EventManager.AlertEmailProcessingFailure += ProcessAlert_EmailProcessingFailure;
            EventManager.IncidentTerminalConfigureFailed += ProcessIncidentTerminalConfigureFailed;
            EventManager.IncidentTerminalRunFailed += ProcessIncidentTerminalRunFailed;
            EventManager.AlertError_EmailSendFailure += ProcessEmailSendFailure;
            EventManager.IncidentTerminalActionActivationFailed += ProcessIncidentTerminalActionActivationFailed;
            EventManager.IncidentTerminalInternalFailureOccurred += ProcessIncidentTerminalInternalFailureOccurred;
            //EventManager.IncidentPluginConfigureFailed += ProcessIncidentPluginConfigureFailed;
            //AlertManager.AlertErrorSyncingCalendar += ProcessErrorSyncingCalendar;
            EventManager.AlertResponseReceived += AlertManagerOnAlertResponseReceived;
            //AlertManager.AlertAttendeeUnresponsivenessThresholdReached += ProcessAttendeeUnresponsivenessThresholdReached;
            //AlertManager.AlertBookingRequestCheckedOut += ProcessBRCheckedOut;
            EventManager.AlertUserRegistrationError += ReportUserRegistrationError;
            //AlertManager.AlertBookingRequestMerged += BookingRequestMerged;
            EventManager.TerminalIncidentReported += LogTerminalIncident;
            EventManager.UnparseableNotificationReceived += LogUnparseableNotificationIncident;
            EventManager.IncidentDocuSignFieldMissing += IncidentDocuSignFieldMissing;
            EventManager.IncidentOAuthAuthenticationFailed += OAuthAuthenticationFailed;
            EventManager.IncidentMissingFieldInPayload += IncidentMissingFieldInPayload;
            EventManager.ExternalEventReceived += LogExternalEventReceivedIncident;
            EventManager.KeyVaultFailure += KeyVaultFailure;
        }

        private void KeyVaultFailure(string keyVaultMethod, Exception ex)
        {
            var incident = new IncidentDO
            {
                CustomerId = "unknown",
                Data = string.Join(Environment.NewLine, "KeyVault method: " + keyVaultMethod, ex.Message, ex.StackTrace ?? ""),
                PrimaryCategory = "KeyVault",
                SecondaryCategory = "QuerySecurePartAsync",
                Component = "Hub",
                Activity = "KeyVault Failed"
            };

            SaveAndLogIncident(incident);
        }

        private void ProcessIncidentTerminalActionActivationFailed(string terminalUrl, string curActionDTO, string objectId)
        {
            var incident = new IncidentDO
            {
                CustomerId = "unknown",
                Data = terminalUrl + "      " + curActionDTO,
                ObjectId = objectId,
                PrimaryCategory = "Action",
                SecondaryCategory = "Activation",
                Activity = "Completed"
            };
            SaveAndLogIncident(incident);
        }

        /// <summary>
        /// Logs incident information using the standard log mechanisms.


        private void SaveAndLogIncident(IncidentDO curIncident)
        {
            SaveIncident(curIncident);
            LogIncident(curIncident);
        }

        private void SaveIncident(IncidentDO curIncident)
        {
            using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.IncidentRepository.Add(curIncident);
                uow.SaveChanges();
            }
        }

        private void LogIncident(IncidentDO curIncident)
        {
            _eventReporter.LogFactInformation(curIncident, curIncident.SecondaryCategory + " " + curIncident.Activity, EventReporter.EventType.Error);
        }

        private void ProcessIncidentTerminalConfigureFailed(string curTerminalUrl, string curAction, string errorMessage, string objectId)
        {
            var incident = new IncidentDO
            {
                CustomerId = "unknown",
                Data = curTerminalUrl + "      " + curAction + " " + errorMessage,
                ObjectId = objectId,
                PrimaryCategory = "Terminal",
                SecondaryCategory = "Configure",
                Component = "Hub",
                Activity = "Configuration Failed"
            };
            SaveAndLogIncident(incident);
        }

        private void ProcessIncidentTerminalInternalFailureOccurred(string curTerminalUrl, string curAction, Exception e, string objectId)
        {
            var incident = new IncidentDO
            {
                CustomerId = "unknown",
                Data = curTerminalUrl + "      " + curAction + " " + e.Message + " \r\nStack trace: \r\n" + e.StackTrace,
                ObjectId = objectId,
                PrimaryCategory = "Terminal",
                SecondaryCategory = "Configure",
                Component = "Terminal",
                Activity = "Configuration Failed"
            };

            // Database is not available from a terminal web application
            // so only log incidents 
            LogIncident(incident);
        }

        private void ProcessIncidentTerminalRunFailed(string curTerminalUrl, string curAction, string errorMessage, string objectId)
        {
            var incident = new IncidentDO
            {
                CustomerId = "unknown",
                Data = curTerminalUrl + "      " + curAction + " " + errorMessage,
                ObjectId = objectId,
                PrimaryCategory = "Terminal",
                SecondaryCategory = "Configure",
                Component = "Hub",
                Activity = "Configuration Failed"
            };
            SaveAndLogIncident(incident);
        }

        private void OAuthAuthenticationFailed(string curRequestQueryString, string errorMessage)
        {
            var incident = new IncidentDO
            {
                CustomerId = "unknown",
                Data = "Query string: " + curRequestQueryString + "      \r\n" + errorMessage,
                ObjectId = "unknown",
                PrimaryCategory = "Terminal",
                SecondaryCategory = "Authentication",
                Activity = "OAuth Authentication Failed"
            };
            SaveAndLogIncident(incident);
        }

        private void LogTerminalIncident(LoggingDataCm incidentItem)
        {
            var currentIncident = new IncidentDO
            {
                ObjectId = incidentItem.ObjectId,
                CustomerId = incidentItem.CustomerId,
                Data = incidentItem.Data,
                PrimaryCategory = incidentItem.PrimaryCategory,
                SecondaryCategory = incidentItem.SecondaryCategory,
                Component = "Terminal",
                Activity = incidentItem.Activity
            };

            SaveAndLogIncident(currentIncident);
        }

        private void LogUnparseableNotificationIncident(string curNotificationUrl, string curNotificationPayload)
        {
            var currentIncident = new IncidentDO
            {
                ObjectId = curNotificationPayload,
                CustomerId = "",
                Data = curNotificationUrl,
                PrimaryCategory = "Event",
                SecondaryCategory = "External",
                Activity = "Unparseble Notification"
            };

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.IncidentRepository.Add(currentIncident);
                uow.SaveChanges();

                GenerateLogData(currentIncident);
            }
        }

        private void LogExternalEventReceivedIncident(string curEventPayload)
        {
            var currentIncident = new IncidentDO
            {
                ObjectId = "EventController",
                CustomerId = "",
                Data = curEventPayload,
                PrimaryCategory = "Event",
                SecondaryCategory = "External",
                Activity = "Received"
            };

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.IncidentRepository.Add(currentIncident);
                uow.SaveChanges();

                GenerateLogData(currentIncident);
            }
        }

        private void GenerateLogData(HistoryItemDO currentIncident)
        {
            string logData = string.Format("{0} {1} {2}:" + " ObjectId: {3} CustomerId: {4}",
                currentIncident.PrimaryCategory,
                currentIncident.SecondaryCategory,
                currentIncident.Activity,
                currentIncident.ObjectId,
                currentIncident.CustomerId);

            Logger.GetLogger().Info(logData);
        }

        private void ProcessAttendeeUnresponsivenessThresholdReached(int expectedResponseId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var expectedResponseDO = uow.ExpectedResponseRepository.GetByKey(expectedResponseId);
                if (expectedResponseDO == null)
                    throw new EntityNotFoundException<ExpectedResponseDO>(expectedResponseId);
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "Negotiation";
                incidentDO.SecondaryCategory = "ClarificationRequest";
                incidentDO.CustomerId = expectedResponseDO.UserID;
                incidentDO.ObjectId = expectedResponseId.ToString();
                incidentDO.Activity = "UnresponsiveAttendee";
                uow.IncidentRepository.Add(incidentDO);
                uow.SaveChanges();
            }
        }

        private void AlertManagerOnAlertResponseReceived(int bookingRequestId, string userID, string customerID)
        {
            using (var _uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "BookingRequest";
                incidentDO.SecondaryCategory = "Response Received";
                incidentDO.CustomerId = customerID;
                incidentDO.ObjectId = bookingRequestId.ToString();
                incidentDO.Activity = "Response Recieved";
                _uow.IncidentRepository.Add(incidentDO);
                _uow.SaveChanges();
            }
        }

        public void ProcessAlert_EmailProcessingFailure(string dateReceived, string errorMessage)
        {
            using (var _uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "Email";
                incidentDO.SecondaryCategory = "Failure";
                incidentDO.Priority = 5;
                incidentDO.Activity = "Intake";
                incidentDO.Data = errorMessage;
                incidentDO.ObjectId = null;
                _uow.IncidentRepository.Add(incidentDO);
                _uow.SaveChanges();
            }
        }

        //public void ProcessBRTimeout(int bookingRequestId, string bookerId)
        //{

        //    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
        //    {
        //        BookingRequestDO bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
        //        IncidentDO incidentDO = new IncidentDO();
        //        incidentDO.PrimaryCategory = "BookingRequest";
        //        incidentDO.SecondaryCategory = null;
        //        incidentDO.Activity = "Timeout";
        //        incidentDO.ObjectId = bookingRequestId.ToString();
        //        incidentDO.CustomerId = bookingRequestDO.CustomerID;
        //        incidentDO.BookerId = bookingRequestDO.BookerID;
        //        uow.IncidentRepository.Add(incidentDO);
        //        uow.SaveChanges();
        //    }
        //}


        private void ProcessEmailSendFailure(int emailId, string message)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "Email";
                incidentDO.SecondaryCategory = "Failure";
                incidentDO.Activity = "Send";
                incidentDO.ObjectId = emailId.ToString();
                incidentDO.Data = message;
                uow.IncidentRepository.Add(incidentDO);
                uow.SaveChanges();
            }
            Email _email = ObjectFactory.GetInstance<Email>();
            //_email.SendAlertEmail("Alert! Kwasant Error Reported: EmailSendFailure",
            //			    string.Format(
            //				  "EmailID: {0}\r\n" +
            //				  "Message: {1}",
            //				  emailId, message));
        }
        //private void ProcessErrorSyncingCalendar(IRemoteCalendarAuthDataDO authData, IRemoteCalendarLinkDO calendarLink = null)
        //{
        //    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
        //    {
        //        IncidentDO incidentDO = new IncidentDO();
        //        incidentDO.PrimaryCategory = "Calendar";
        //        incidentDO.SecondaryCategory = "Failure";
        //        incidentDO.Activity = "Synchronization";
        //        incidentDO.ObjectId = authData.Id.ToString();
        //        incidentDO.CustomerId = authData.UserID;
        //        if (calendarLink != null)
        //        {
        //            incidentDO.Data = string.Format("Link #{0}: {1}", calendarLink.Id, calendarLink.LastSynchronizationResult);
        //        }
        //        uow.IncidentRepository.Add(incidentDO);
        //        uow.SaveChanges();
        //    }

        //    var emailBodyBuilder = new StringBuilder();
        //    emailBodyBuilder.AppendFormat("CalendarSync failure for calendar auth data #{0} ({1}):\r\n", authData.Id,
        //                                  authData.Provider.Name);
        //    emailBodyBuilder.AppendFormat("Customer id: {0}\r\n", authData.UserID);
        //    if (calendarLink != null)
        //    {
        //        emailBodyBuilder.AppendFormat("Calendar link id: {0}\r\n", calendarLink.Id);
        //        emailBodyBuilder.AppendFormat("Local calendar id: {0}\r\n", calendarLink.LocalCalendarID);
        //        emailBodyBuilder.AppendFormat("Remote calendar url: {0}\r\n", calendarLink.RemoteCalendarHref);
        //        emailBodyBuilder.AppendFormat("{0}\r\n", calendarLink.LastSynchronizationResult);
        //    }

        //    Email email = ObjectFactory.GetInstance<Email>();
        //   // email.SendAlertEmail("CalendarSync failure", emailBodyBuilder.ToString());
        //}

        //public void ProcessSubmittedNote(int bookingRequestId, string note)
        //{
        //    if (String.IsNullOrEmpty(note))
        //        throw new ArgumentException("Empty note.", "note");
        //    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
        //    {
        //        var curBookingRequest = uow.BookingRequestRepository.GetByKey(bookingRequestId);
        //        if (curBookingRequest == null)
        //            throw new EntityNotFoundException<BookingRequestDO>(bookingRequestId);
        //        var incidentDO = new IncidentDO
        //            {
        //                PrimaryCategory = "BookingRequest",
        //                SecondaryCategory = "Note",
        //                Activity = "Created",
        //                BookerId = curBookingRequest.BookerID,
        //                ObjectId = bookingRequestId.ToString(),
        //                Data = note
        //            };
        //        uow.IncidentRepository.Add(incidentDO);
        //        uow.SaveChanges();
        //    }
        //}

        //public void ProcessBRCheckedOut(int bookingRequestId, string bookerId)
        //{
        //    //BookingRequest _br = new BookingRequest();
        //    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
        //    {
        //        var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
        //        if (bookingRequestDO == null)
        //            throw new ArgumentException(string.Format("Cannot find a Booking Request by given id:{0}", bookingRequestId), "bookingRequestId");
        //        string status = bookingRequestDO.BookingRequestStateTemplate.Name;
        //        IncidentDO curAction = new IncidentDO()
        //        {
        //            PrimaryCategory = "BookingRequest",
        //            SecondaryCategory = null,
        //            Activity = "Checkout",
        //            CustomerId = bookingRequestDO.Customer.Id,
        //            ObjectId = bookingRequestId.ToString(),
        //            BookerId = bookerId,
        //        };

        //       // int getMinutinQueue = _br.GetTimeInQueue(uow, bookingRequestDO.Id.ToString());

        //        //curAction.Data = string.Format("Time To Process: {0}", getMinutinQueue);

        //        //uow.IncidentRepository.Add(curAction);
        //        uow.SaveChanges();
        //    }
        //}

        //private void ProcessBRMarkedProcessed(int bookingRequestId, string bookerId)
        //{
        //    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
        //    {
        //        var bookingRequestDO = uow.BookingRequestRepository.GetByKey(bookingRequestId);
        //        if (bookingRequestDO == null)
        //            throw new ArgumentException(string.Format("Cannot find a Booking Request by given id:{0}", bookingRequestId), "bookingRequestId");
        //        IncidentDO curAction = new IncidentDO()
        //        {
        //            PrimaryCategory = "BookingRequest",
        //            SecondaryCategory = "BookerAction",
        //            Activity = "MarkedAsProcessed",
        //            CustomerId = bookingRequestDO.CustomerID,
        //            ObjectId = bookingRequestId.ToString(),
        //            BookerId = bookerId,
        //        };

        //       // var br = ObjectFactory.GetInstance<BookingRequest>();
        //       // int getMinutinQueue = br.GetTimeInQueue(uow, bookingRequestDO.Id.ToString());

        //       // curAction.Data = string.Format("Time To Process: {0}", getMinutinQueue);
        //        uow.IncidentRepository.Add(curAction);
        //        uow.SaveChanges();
        //    }
        //}

        public void ReportUserRegistrationError(Exception ex)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "DockYardAccount";
                incidentDO.SecondaryCategory = "Error";
                incidentDO.Activity = "Registration";
                incidentDO.Data = ex.Message;
                uow.IncidentRepository.Add(incidentDO);
                uow.SaveChanges();

                GenerateLogData(incidentDO);
            }
        }

        public void BookingRequestMerged(int originalBRId, int targetBRId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "BookingRequest";
                incidentDO.SecondaryCategory = "BookerAction";
                incidentDO.Activity = "MergedBRs";
                incidentDO.ObjectId = originalBRId.ToString();

                string logData = string.Format("{0} {1} {2}: ",
                        incidentDO.PrimaryCategory,
                        incidentDO.SecondaryCategory,
                        incidentDO.Activity);

                incidentDO.Data = logData + incidentDO.ObjectId;
                uow.IncidentRepository.Add(incidentDO);
                Logger.GetLogger().Info(incidentDO.Data);
                uow.SaveChanges();

                incidentDO.ObjectId = targetBRId.ToString();
                incidentDO.Data = logData + incidentDO.ObjectId;
                uow.IncidentRepository.Add(incidentDO);
                Logger.GetLogger().Info(incidentDO.Data);
                uow.SaveChanges();
            }
        }

        public void IncidentDocuSignFieldMissing(string envelopeId, string fieldName)
        {
            using (var _uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "Envelope";
                incidentDO.SecondaryCategory = "";
                incidentDO.ObjectId = envelopeId;
                incidentDO.Activity = "Action processing";
                incidentDO.Data = String.Format("IncidentDocuSignFieldMissing: Envelope id: {0}, Field name: {1}", envelopeId, fieldName);
                _uow.IncidentRepository.Add(incidentDO);
                Logger.GetLogger().Warn(incidentDO.Data);
                _uow.SaveChanges();
            }
        }

        public void IncidentMissingFieldInPayload(string fieldKey, ActionDO action, string curUserId)
        {
            using (var _uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                IncidentDO incidentDO = new IncidentDO();
                incidentDO.PrimaryCategory = "Process Execution";
                incidentDO.SecondaryCategory = "Action";
                incidentDO.ObjectId = action.Id.ToString();
                incidentDO.Activity = "Occured";
                incidentDO.CustomerId = curUserId;
                incidentDO.Data = String.Format("MissingFieldInPayload: ActionName: {0}, Field name: {1}, ActionId {2}", action.Name, fieldKey, action.Id);
                _uow.IncidentRepository.Add(incidentDO);
                Logger.GetLogger().Warn(incidentDO.Data);
                _uow.SaveChanges();
            }
        }

    }
}
