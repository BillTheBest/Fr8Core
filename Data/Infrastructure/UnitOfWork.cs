﻿﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Transactions;
using Data.Interfaces;
using Data.Repositories;

namespace Data.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private TransactionScope _transaction;
        private readonly IDBContext _context;

        internal UnitOfWork(IDBContext context)
        {
            _context = context;
            _context.UnitOfWork = this;
        }

        private AttachmentRepository _attachmentRepository;

        public AttachmentRepository AttachmentRepository
        {
            get
            {
                return _attachmentRepository ?? (_attachmentRepository = new AttachmentRepository(this));
            }
        }

        //private AttendeeRepository _attendeeRepository;

        //public AttendeeRepository AttendeeRepository
        //{
        //    get
        //    {
        //        return _attendeeRepository ?? (_attendeeRepository = new AttendeeRepository(this));
        //    }
        //}

        private EmailAddressRepository _emailAddressRepository;

        public EmailAddressRepository EmailAddressRepository
        {
            get
            {
                return _emailAddressRepository ?? (_emailAddressRepository = new EmailAddressRepository(this));
            }
        }

        private RecipientRepository _recipientRepository;
        public RecipientRepository RecipientRepository
        {
            get
            {
                return _recipientRepository ?? (_recipientRepository = new RecipientRepository(this));
            }
        }
        

        private SlipRepository _SlipRepository;

        public SlipRepository SlipRepository
        {
            get
            {
                return _SlipRepository ?? (_SlipRepository = new SlipRepository(this));
            }
        }

        private RemoteServiceProviderRepository _remoteServiceProviderRepository;

        public RemoteServiceProviderRepository RemoteServiceProviderRepository
        {
            get
            {
                return _remoteServiceProviderRepository ?? (_remoteServiceProviderRepository = new RemoteServiceProviderRepository(this));
            }
        }

        private RemoteServiceAuthDataRepository _remoteServiceAuthDataRepository;

        public RemoteServiceAuthDataRepository RemoteServiceAuthDataRepository
        {
            get
            {
                return _remoteServiceAuthDataRepository ?? (_remoteServiceAuthDataRepository = new RemoteServiceAuthDataRepository(this));
            }
        }

        
        private CommunicationConfigurationRepository _communicationConfigurationRepository;

        public CommunicationConfigurationRepository CommunicationConfigurationRepository
        {
            get
            {
                return _communicationConfigurationRepository ??
                       (_communicationConfigurationRepository = new CommunicationConfigurationRepository(this));
            }
        }

        private EmailRepository _emailRepository;

        public EmailRepository EmailRepository
        {
            get
            {
                return _emailRepository ?? (_emailRepository = new EmailRepository(this));
            }
        }

        private IProcessRepository _processRepository;

        public IProcessRepository ProcessRepository
        {
            get
            {
                return _processRepository ?? (_processRepository = new ProcessRepository(this));
            }
        }
        private EmailStatusRepository _emailStatusRepository;

        public EmailStatusRepository EmailStatusRepository
        {
            get
            {
                return _emailStatusRepository ?? (_emailStatusRepository = new EmailStatusRepository(this));
            }
        }

        private EnvelopeRepository _envelopeRepository;

        public EnvelopeRepository EnvelopeRepository
        {
            get
            {
                return _envelopeRepository ?? (_envelopeRepository = new EnvelopeRepository(this));
            }
        }


        private MailerRepository _mailerRepository;

        public MailerRepository MailerRepository
        {
            get
            {
                return _mailerRepository ?? (_mailerRepository = new MailerRepository(this));
            }
        }


        private EventStatusRepository _eventStatusRepository;

        public EventStatusRepository EventStatusRepository
        {
            get
            {
                return _eventStatusRepository ?? (_eventStatusRepository = new EventStatusRepository(this));
            }
        }

        private InstructionRepository _instructionRepository;

        public InstructionRepository InstructionRepository
        {
            get
            {
                return _instructionRepository ?? (_instructionRepository = new InstructionRepository(this));
            }
        }

        private InvitationRepository _invitationRepository;

        public InvitationRepository InvitationRepository
        {
            get
            {
                return _invitationRepository ?? (_invitationRepository = new InvitationRepository(this));
            }
        }

    

        private StoredFileRepository _storedFileRepository;

        public StoredFileRepository StoredFileRepository
        {
            get
            {
                return _storedFileRepository ?? (_storedFileRepository = new StoredFileRepository(this));
            }
        }

        private TrackingStatusRepository _trackingStatusRepository;

        public TrackingStatusRepository TrackingStatusRepository
        {
            get
            {
                return _trackingStatusRepository ?? (_trackingStatusRepository = new TrackingStatusRepository(this));
            }
        }

        private HistoryRepository _historyRepository;

        public HistoryRepository HistoryRepository
        {
            get
            {
                return _historyRepository ?? (_historyRepository = new HistoryRepository(this));
            }
        }

        private FactRepository _factRepository;
        
        public FactRepository FactRepository
        {
            get
            {
                return _factRepository ?? (_factRepository = new FactRepository(this));
            }
        }
     
        private UserRepository _userRepository;

        public UserRepository UserRepository
        {
            get
            {
                return _userRepository ?? (_userRepository = new UserRepository(this));
            }
        }

        private UserStatusRepository _userStatusRepository;

        public UserStatusRepository UserStatusRepository
        {
            get
            {
                return _userStatusRepository ?? (_userStatusRepository = new UserStatusRepository(this));
            }
        }

        //private NegotiationAnswerEmailRepository _negotiationAnswerEmailRepository;

        //public NegotiationAnswerEmailRepository NegotiationAnswerEmailRepository
        //{
        //    get
        //    {
        //        return _negotiationAnswerEmailRepository ?? (_negotiationAnswerEmailRepository = new NegotiationAnswerEmailRepository(this));
        //    }
        //}

        private UserAgentInfoRepository _userAgentInfoRepository;

        public UserAgentInfoRepository UserAgentInfoRepository
        {
            get
            {
                return _userAgentInfoRepository ?? (_userAgentInfoRepository = new UserAgentInfoRepository(this));
            }
        }

        private AspNetUserRolesRepository _aspNetUserRolesRepository;

        public AspNetUserRolesRepository AspNetUserRolesRepository
        {
            get
            {
                return _aspNetUserRolesRepository ?? (_aspNetUserRolesRepository = new AspNetUserRolesRepository(this));
            }
        }

        private AspNetRolesRepository _aspNetRolesRepository;

        public AspNetRolesRepository AspNetRolesRepository
        {
            get
            {
                return _aspNetRolesRepository ?? (_aspNetRolesRepository = new AspNetRolesRepository(this));
            }
        }

        private IncidentRepository _incidentRepository;

        public IncidentRepository IncidentRepository
        {
            get
            {
                return _incidentRepository ?? (_incidentRepository = new IncidentRepository(this));
            }
        }

        //private QuestionRepository _questionRepository;

        //public QuestionRepository QuestionRepository
        //{
        //    get
        //    {
        //        return _questionRepository ?? (_questionRepository = new QuestionRepository(this));
        //    }
        //}

        //private AnswerRepository _answerRepository;

        //public AnswerRepository AnswerRepository
        //{
        //    get
        //    {
        //        return _answerRepository ?? (_answerRepository = new AnswerRepository(this));
        //    }
        //}

        //private QuestionResponseRepository _questionResponseRepository;

        //public QuestionResponseRepository QuestionResponseRepository
        //{
        //    get
        //    {
        //        return _questionResponseRepository ?? (_questionResponseRepository = new QuestionResponseRepository(this));
        //    }
        //}


        //private NegotiationsRepository _negotiationsRepository;

        //public NegotiationsRepository NegotiationsRepository
        //{
        //    get
        //    {
        //        return _negotiationsRepository ?? (_negotiationsRepository = new NegotiationsRepository(this));
        //    }
        //}

        //private QuestionsRepository _questionsRepository;

        //public QuestionsRepository QuestionsRepository
        //{
        //    get
        //    {
        //        return _questionsRepository ?? (_questionsRepository = new QuestionsRepository(this));
        //    }
        //}

        private AuthorizationTokenRepository _authorizationTokenRepository;

        public AuthorizationTokenRepository AuthorizationTokenRepository
        {
            get
            {
                return _authorizationTokenRepository ?? (_authorizationTokenRepository = new AuthorizationTokenRepository(this));
            }
        }

        private LogRepository _logRepository;

        public LogRepository LogRepository
        {
            get
            {
                return _logRepository ?? (_logRepository = new LogRepository(this));
            }
        }

        private ProfileNodeRepository _profileNodeRepository;

        public ProfileNodeRepository ProfileNodeRepository
        {
            get
            {
                return _profileNodeRepository ?? (_profileNodeRepository = new ProfileNodeRepository(this));
            }
        }

        private ProfileItemRepository _profileItemRepository;

        public ProfileItemRepository ProfileItemRepository
        {
            get
            {
                return _profileItemRepository ?? (_profileItemRepository = new ProfileItemRepository(this));
            }
        }

        private ProfileRepository _profileRepository;

        public ProfileRepository ProfileRepository
        {
            get
            {
                return _profileRepository ?? (_profileRepository = new ProfileRepository(this));
            }
        }

        private ExpectedResponseRepository _expectedResponseRepository;
        public ExpectedResponseRepository ExpectedResponseRepository
        {
            get
            {
                return _expectedResponseRepository ?? (_expectedResponseRepository = new ExpectedResponseRepository(this));
            }
        }

	  private ActionRepository _actionRepository;
	  public ActionRepository ActionRepository
        {
            get
            {
                return _actionRepository ?? (_actionRepository = new ActionRepository(this));
            }
        }

        private ActivityTemplateRepository _activityTemplateRepository;
        public ActivityTemplateRepository ActivityTemplateRepository
        {
            get
            {
                return _activityTemplateRepository ?? (_activityTemplateRepository = new ActivityTemplateRepository(this));
            }
        }

	  private ActionListRepository _actionListRepository;
	  public ActionListRepository ActionListRepository
        {
            get
            {
                return _actionListRepository ?? (_actionListRepository = new ActionListRepository(this));
            }
        }
	  private ActivityRepository _activityRepository;
	  public ActivityRepository ActivityRepository
	  {
		  get
		  {
			  return _activityRepository ?? (_activityRepository = new ActivityRepository(this));
		  }
	  }
      private IProcessTemplateRepository _processTemplateRepository;

        public IProcessTemplateRepository ProcessTemplateRepository
        {
            get
            {
                return _processTemplateRepository ?? (_processTemplateRepository = new ProcessTemplateRepository(this));
            }
        }

		private ProcessNodeRepository _proeProcessNodeRepository;

        public ProcessNodeRepository ProcessNodeRepository
        {
            get
            {
                return _proeProcessNodeRepository ?? (_proeProcessNodeRepository = new ProcessNodeRepository(this));
            }
        }

        private ExternalEventSubscriptionRepository _externalEventSubscriptionRepository;

	    public ExternalEventSubscriptionRepository ExternalEventSubscriptionRepository
	    {
		    get
		    {
		        return _externalEventSubscriptionRepository ?? (_externalEventSubscriptionRepository = new ExternalEventSubscriptionRepository(this));
		    }
	    }

	    private ProcessNodeTemplateRepository _processNodeTemplateRepository;

        public IProcessNodeTemplateRepository ProcessNodeTemplateRepository
        {
            get
            {
                return _processNodeTemplateRepository ?? (_processNodeTemplateRepository = new ProcessNodeTemplateRepository(this));
            }
        }


        private CriteriaRepository _criteriaRepository;

        public ICriteriaRepository CriteriaRepository
        {
            get
            {
                return _criteriaRepository ?? (_criteriaRepository = new CriteriaRepository(this));
            }
        }

        private FileRepository _fileRepository;

        public IFileRepository FileRepository
        {
            get
            {
                return _fileRepository ?? (_fileRepository = new FileRepository(this));
            }
        }

        private MTFieldRepository _mtFieldRepository;

        public IMTFieldRepository MTFieldRepository
        {
            get
            {
                return _mtFieldRepository ?? (_mtFieldRepository = new MTFieldRepository(this));
            }
        }

        private MTObjectRepository _mtObjectdRepository;

        public IMTObjectRepository MTObjectRepository
        {
            get
            {
                return _mtObjectdRepository ?? (_mtObjectdRepository = new MTObjectRepository(this));
            }
        }

        private MTOrganizationRepository _mtOrganizationdRepository;

        public IMTOrganizationRepository MTOrganizationRepository
        {
            get
            {
                return _mtOrganizationdRepository ?? (_mtOrganizationdRepository = new MTOrganizationRepository(this));
            }
        }

        private MTDataRepository _mtDataRepository;

        public IMTDataRepository MTDataRepository
        {
            get
            {
                return _mtDataRepository ?? (_mtDataRepository = new MTDataRepository(this));
            }
        }


        private PluginRepository _pluginRepository;

        public IPluginRepository PluginRepository
        {
            get
            {
                return _pluginRepository ?? (_pluginRepository = new PluginRepository(this));
            }
        }


        private SubscriptionRepository _subscriptionRepository;

        public ISubscriptionRepository SubscriptionRepository
        {
            get
            {
                return _subscriptionRepository ?? (_subscriptionRepository = new SubscriptionRepository(this));
            }
        }

	    public void Save()
        {
            _context.SaveChanges();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_transaction != null)
                _transaction.Dispose();
            _context.Dispose();
        }

        public void StartTransaction()
        {
            _transaction = new TransactionScope();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Commit()
        {
            SaveChanges();
            _transaction.Complete();
            _transaction.Dispose();
        }

        public void SaveChanges()
        {
            _context.DetectChanges();
            var addedEntities = _context.AddedEntities;
            var modifiedEntities = _context.ModifiedEntities;
            var deletedEntities = _context.DeletedEntities;

            try
            {
                _context.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                string errorFormat = @"Validation failed for entity [{0}]. Validation errors:" + Environment.NewLine + @"{1}";
                var errorList = new List<String>();
                foreach (var entityValidationError in e.EntityValidationErrors)
                {
                    var entityName = entityValidationError.Entry.Entity.GetType().Name;
                    var errors = String.Join(Environment.NewLine, entityValidationError.ValidationErrors.Select(a => a.PropertyName + ": " + a.ErrorMessage));
                    errorList.Add(String.Format(errorFormat, entityName, errors));
                }
                throw new Exception(String.Join(Environment.NewLine + Environment.NewLine, errorList) + Environment.NewLine, e);
            }

            OnEntitiesAdded(new EntitiesStateEventArgs(this, addedEntities));
            OnEntitiesModified(new EntitiesStateEventArgs(this, modifiedEntities));
            OnEntitiesDeleted(new EntitiesStateEventArgs(this, deletedEntities));
        }

        public bool IsEntityModified<TEntity>(TEntity entity) 
            where TEntity : class
        {
            return _context.Entry(entity).State == EntityState.Modified;
        }

        public IDBContext Db
        {
            get { return _context; }
        }

        /// <summary>
        /// Occurs for entities added after they saved to db.
        /// </summary>
        public static event EntitiesStateHandler EntitiesAdded;
        /// <summary>
        /// Occurs for entities modified after they saved to db.
        /// </summary>
        public static event EntitiesStateHandler EntitiesModified;
        /// <summary>
        /// Occurs for entities deleted after they removed from db.
        /// </summary>
        public static event EntitiesStateHandler EntitiesDeleted;

        private static void OnEntitiesAdded(EntitiesStateEventArgs args)
        {
            if (args.Entities == null || args.Entities.Length == 0)
                return;
            EntitiesStateHandler handler = EntitiesAdded;
            if (handler != null) handler(null, args);
        }

        private static void OnEntitiesModified(EntitiesStateEventArgs args)
        {
            if (args.Entities == null || args.Entities.Length == 0)
                return;
            EntitiesStateHandler handler = EntitiesModified;
            if (handler != null) handler(null, args);
        }

        private static void OnEntitiesDeleted(EntitiesStateEventArgs args)
        {
            if (args.Entities == null || args.Entities.Length == 0)
                return;
            EntitiesStateHandler handler = EntitiesDeleted;
            if (handler != null) handler(null, args);
        }

    }

    public delegate void EntitiesStateHandler(object sender, EntitiesStateEventArgs args);

    public class EntitiesStateEventArgs : EventArgs
    {
        public IUnitOfWork UnitOfWork { get; private set; }
        public object[] Entities { get; private set; }

        public EntitiesStateEventArgs(IUnitOfWork unitOfWork, object[] entities)
        {
            UnitOfWork = unitOfWork;
            Entities = entities;
        }
    }
}
