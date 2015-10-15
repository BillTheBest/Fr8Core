﻿using System;
using Data.Repositories;
using StructureMap;

namespace Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {

        AttachmentRepository AttachmentRepository { get; }
        //AttendeeRepository AttendeeRepository { get; }
        EmailAddressRepository EmailAddressRepository { get; }
        RecipientRepository RecipientRepository { get; }
        //BookingRequestRepository BookingRequestRepository { get; }
        //BookingRequestStatusRepository BookingRequestStatusRepository { get; }
        //CalendarRepository CalendarRepository { get; }
        CommunicationConfigurationRepository CommunicationConfigurationRepository { get; }
        EmailRepository EmailRepository { get; }
        IProcessRepository ProcessRepository { get; }
        EmailStatusRepository EmailStatusRepository { get; }
        //EnvelopeRepository EnvelopeRepository { get; }
        //EventRepository EventRepository { get; }
        InstructionRepository InstructionRepository { get; }
        InvitationRepository InvitationRepository { get; }
        //InvitationResponseRepository InvitationResponseRepository { get; }
        StoredFileRepository StoredFileRepository { get; }
        TrackingStatusRepository TrackingStatusRepository { get; }
        UserAgentInfoRepository UserAgentInfoRepository { get; }
        UserRepository UserRepository { get; }
        AspNetUserRolesRepository AspNetUserRolesRepository { get; }
        AspNetRolesRepository AspNetRolesRepository { get; }
        IncidentRepository IncidentRepository { get; }
        //NegotiationsRepository NegotiationsRepository { get; }
        //QuestionsRepository QuestionsRepository { get; }
        FactRepository FactRepository { get; }
        //QuestionRepository QuestionRepository { get; }
        //AnswerRepository AnswerRepository { get; }
        //QuestionResponseRepository QuestionResponseRepository { get; }
        AuthorizationTokenRepository AuthorizationTokenRepository { get; }
        LogRepository LogRepository { get; }
        ProfileNodeRepository ProfileNodeRepository { get; }
        ProfileItemRepository ProfileItemRepository { get; }
        ProfileRepository ProfileRepository { get; }
        UserStatusRepository UserStatusRepository { get; }
        //NegotiationAnswerEmailRepository NegotiationAnswerEmailRepository { get; }
        ExpectedResponseRepository ExpectedResponseRepository { get; }
        IRouteRepository RouteRepository { get; }
        SlipRepository SlipRepository { get; }
        ActionRepository ActionRepository { get; }
        ActivityTemplateRepository ActivityTemplateRepository { get; }
		  ActivityRepository ActivityRepository { get; }
        ProcessNodeRepository ProcessNodeRepository { get; }

        ISubrouteRepository SubrouteRepository { get; }
        ICriteriaRepository CriteriaRepository { get; }

        IFileRepository FileRepository { get; }

        IMTFieldRepository MTFieldRepository { get; }

        IMTObjectRepository MTObjectRepository { get; }

        IMTOrganizationRepository MTOrganizationRepository { get; }

        IMTFieldTypeRepository MTFieldTypeRepository { get; }

        IMTDataRepository MTDataRepository { get; }

        MultiTenantObjectRepository MultiTenantObjectRepository { get; }

        IPluginRepository PluginRepository { get; }
        ISubscriptionRepository SubscriptionRepository { get; }

	    /// <summary>
        /// Call this to commit the unit of work
        /// </summary>
        void Commit();

        /// <summary>
        /// Return the database reference for this UOW
        /// </summary>
        IDBContext Db { get; }

        RemoteServiceProviderRepository RemoteServiceProviderRepository { get; }
        RemoteServiceAuthDataRepository RemoteServiceAuthDataRepository { get; }
        //RemoteCalendarLinkRepository RemoteCalendarLinkRepository { get; }
        HistoryRepository HistoryRepository { get; }
        EnvelopeRepository EnvelopeRepository { get; }

        /// <summary>
        /// Starts a transaction on this unit of work
        /// </summary>
        void StartTransaction();

        /// <summary>
        /// The save changes.
        /// </summary>
        void SaveChanges();

        /// <summary>
        /// The save changes.
        /// </summary>
        // void SaveChanges(SaveOptions saveOptions);

        bool IsEntityModified<TEntity>(TEntity entity)
            where TEntity : class;
    }
}
