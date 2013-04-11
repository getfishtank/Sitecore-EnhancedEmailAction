using System;
using System.Net.Mail;
using System.Web;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Security;
using Sitecore.Security.Accounts;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;

namespace Fishtank.SharedSource.Workflow.Actions
{
    public class EnhancedEmailAction
    {
        public void Process(WorkflowPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            ProcessorItem processorItem = args.ProcessorItem;
            if (processorItem != null)
            {
                Item innerItem = processorItem.InnerItem;
                string fullPath = innerItem.Paths.FullPath;
                string from = GetText(innerItem, "from", args);
                string to = GetText(innerItem, "to", args);
                string host = GetText(innerItem, "mail server", args);
                string subject = GetText(innerItem, "subject", args);
                string body = GetText(innerItem, "message", args);
                Error.Assert(to.Length > 0, "The 'To' field is not specified in the mail action item: " + fullPath);
                Error.Assert(from.Length > 0, "The 'From' field is not specified in the mail action item: " + fullPath);
                Error.Assert(subject.Length > 0,
                             "The 'Subject' field is not specified in the mail action item: " + fullPath);
                Error.Assert(host.Length > 0,
                             "The 'Mail server' field is not specified in the mail action item: " + fullPath);
                var message = new MailMessage(from, to);
                message.Subject = subject;
                message.Body = body;
                new SmtpClient(host).Send(message);
            }
        }

        private string GetText(Item commandItem, string field, WorkflowPipelineArgs args)
        {
            string text = commandItem[field];
            if (text.Length > 0)
            {
                return ReplaceVariables(text, args);
            }
            return string.Empty;
        }

        private string ReplaceVariables(string text, WorkflowPipelineArgs args)
        {
            Item workflowItem = args.DataItem;

            text = text.Replace("$itemPath$", workflowItem.Paths.FullPath);
            text = text.Replace("$itemLanguage$", workflowItem.Language.ToString());
            text = text.Replace("$itemVersion$", workflowItem.Version.ToString());
            text = text.Replace("$itemSubmittedBy$", GetSubmitter(args));
            text = text.Replace("$itemComments$", args.Comments);


            bool itemHasLayout = !String.IsNullOrEmpty(workflowItem.Fields[FieldIDs.LayoutField].Value);
            bool stdValuesHasLayout = (workflowItem.Fields[FieldIDs.LayoutField].ContainsStandardValue &&
                                       workflowItem.Template.StandardValues != null);
            if (stdValuesHasLayout)
                stdValuesHasLayout =
                    !String.IsNullOrEmpty(workflowItem.Template.StandardValues.Fields[FieldIDs.LayoutField].Value);

            if (itemHasLayout || stdValuesHasLayout)
                text = text.Replace("$itemPreviewUrl$",
                                    String.Format("{0}://{1}/?sc_itemid=%7b{2}%7d&sc_mode=preview&sc_lang={3}",
                                                  HttpContext.Current.Request.Url.Scheme,
                                                  HttpContext.Current.Request.Url.Host,
                                                  workflowItem.ID.Guid.ToString().ToUpper(), 
                                                  workflowItem.Language.Name));
            else
                text = text.Replace("$itemPreviewUrl$", "This item is not a page and cannot be previewed.");

            return text;
        }

        private string GetSubmitter(WorkflowPipelineArgs args)
        {
            string result = String.Empty;

            Item contentItem = args.DataItem;
            IWorkflow contentWorkflow = contentItem.Database.WorkflowProvider.GetWorkflow(contentItem);
            WorkflowEvent[] contentHistory = contentWorkflow.GetHistory(contentItem);

            if (contentHistory.Length > 0)
            {
                string lastUser = contentHistory[contentHistory.Length - 1].User;
                User user = User.FromName(lastUser, false);
                UserProfile userProfile = user.Profile;

                result = userProfile.FullName;
            }

            return result;
        }
    }
}