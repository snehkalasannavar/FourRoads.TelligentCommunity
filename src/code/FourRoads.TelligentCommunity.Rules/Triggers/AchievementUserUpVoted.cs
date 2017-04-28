using System;
using System.Collections.Generic;
using FourRoads.Common.TelligentCommunity.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Rules.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace FourRoads.TelligentCommunity.Rules.Triggers
{
    public class AchievementUserUpVoted : IRuleTrigger, ITranslatablePlugin, ISingletonPlugin, ICategorizedPlugin
    {
        private IRuleController _ruleController;
        private ITranslatablePluginController _translationController;
        private readonly Guid _triggerid = new Guid("{F84CEAB2-98B8-41FB-A810-0B73A1E03F8C}");

        public void Initialize()
        {
            Apis.Get<IForumReplyVotes>().Events.AfterCreate += EventsOnAfterCreate;
            Apis.Get<IForumReplyVotes>().Events.AfterUpdate += EventsOnAfterUpdate;
        }

        /// <summary>
        /// Check on the action performed
        /// For some reason this event is also called when updating ?
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnAfterCreate(ForumReplyVoteAfterCreateEventArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    if (args.Value)
                    {
                        var usersUpVotes =
                            Apis.Get<IForumReplyVotes>()
                                .List(new ForumReplyVoteListOptions()
                                {
                                    UserId = args.UserId,
                                    VoteType = "Quality",
                                    Value = true
                                });
                        if (!usersUpVotes.HasErrors())
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", args.UserId.ToString()
                                },
                                {
                                    "ReplyId", args.ReplyId.ToString()
                                },
                                {
                                    "Total" , usersUpVotes.Count.ToString()
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterCreate failed for userid:{0}", args.UserId),
                    ex).Log();
            }
        }

        /// <summary>
        /// Check on the action performed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void EventsOnAfterUpdate(ForumReplyVoteAfterUpdateEventArgs args)
        {
            try
            {
                if (_ruleController != null)
                {
                    if (args.Value)
                    {
                        var usersUpVotes =
                            Apis.Get<IForumReplyVotes>()
                                .List(new ForumReplyVoteListOptions()
                                {
                                    UserId = args.UserId,
                                    VoteType = "Quality",
                                    Value = true
                                });
                        if (!usersUpVotes.HasErrors())
                        {
                            _ruleController.ScheduleTrigger(new Dictionary<string, string>()
                            {
                                {
                                    "UserId", args.UserId.ToString()
                                },
                                {
                                    "ReplyId", args.ReplyId.ToString()
                                },
                                {
                                    "Total" , usersUpVotes.Count.ToString()
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new TCException(
                    string.Format("EventsOnAfterUpdate failed for userid:{0}", args.UserId),
                    ex).Log();
            }
        }

        public string Name
        {
            get { return "4 Roads - Achievements - User Up Voted Trigger"; }
        }

        public string Description
        {
            get { return "Fires when a user is up voted to allow an achievement to be assigned based on the total up votes a user has recieved."; }
        }

        public void SetController(IRuleController controller)
        {
            _ruleController = controller;
        }

        public RuleTriggerExecutionContext GetExecutionContext(RuleTriggerData data)
        {
            RuleTriggerExecutionContext context = new RuleTriggerExecutionContext();
            if (data.ContainsKey("UserId"))
            {
                int userId;

                if (int.TryParse(data["UserId"], out userId))
                {
                    var users = Apis.Get<IUsers>();
                    var user = users.Get(new UsersGetOptions() { Id = userId });

                    if (!user.HasErrors())
                    {
                        context.Add(users.ContentTypeId, user);
                        context.Add(_triggerid, true); //Added this trigger so that it is not re-entrant
                    }
                }
            }

            if (data.ContainsKey("ReplyId"))
            {
                int replyId;
                if (int.TryParse(data["ReplyId"], out replyId))
                {
                    var forumReplies = Apis.Get<IForumReplies>();
                    var forumReply = forumReplies.Get(replyId);

                    if (!forumReply.HasErrors())
                    {
                        context.Add(forumReply.GlobalContentTypeId, forumReply);
                    }
                }
            }
            return context;
        }

        public Guid RuleTriggerId
        {
            get { return _triggerid; }
        }

        public string RuleTriggerName
        {
            get { return _translationController.GetLanguageResourceValue("RuleTriggerName"); }
        }

        public string RuleTriggerCategory
        {
            get { return _translationController.GetLanguageResourceValue("RuleTriggerCategory"); }
        }

        /// <summary>
        /// Setup the contextual datatype ids for users, forum reply and custom trigger parameters
        /// custom trigger parameters are to allow config in the UI for the action being performed (add-upvote, del-upvote etc)
        /// </summary>
        /// <returns>IEnumerable<Guid></returns>
        /// 
        public IEnumerable<Guid> ContextualDataTypeIds
        {
            get { return new[] { Apis.Get<IUsers>().ContentTypeId, Apis.Get<IForumReplies>().ContentTypeId }; }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            _translationController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                Translation[] defaultTranslation = new[] { new Translation("en-us") };

                defaultTranslation[0].Set("RuleTriggerName", "[achievement] a user has recieved a number of up votes");
                defaultTranslation[0].Set("RuleTriggerCategory", "Forum Reply");

                return defaultTranslation;
            }
        }

        public string[] Categories
        {
            get
            {
                return new[]
                {
                    "Rules"
                };
            }
        }

    }
}
