﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TechTalk.SpecFlow;
using Vermaat.Crm.Specflow.Commands;

namespace Vermaat.Crm.Specflow.Steps
{
    [Binding]
    [DeploymentItem("DefaultData.xml")]
    public class GeneralSteps
    {
        private readonly CrmTestingContext _crmContext;
        private readonly SeleniumTestingContext _seleniumContext;

        public GeneralSteps(CrmTestingContext crmContext, SeleniumTestingContext seleniumContext)
        {
            _crmContext = crmContext;
            _seleniumContext = seleniumContext;
        }


        #region Given

        [Given(@"an existing ([^\s]+) named (.*) with the following values")]
        public void GivenExistingWithValues(string entityName, string alias, Table criteria)
        {
            Entity entity = ThenRecordExists(entityName, criteria);
            _crmContext.RecordCache.Add(alias, entity);
        }

        [Given(@"a ([^\s]+) named (.*) with the following values")]
        [Given(@"an ([^\s]+) named (.*) with the following values")]
        [When(@"a ([^\s]+) named (.*) is created with the following values")]
        [When(@"an ([^\s]+) named (.*) is created with the following values")]
        public void GivenEntityWithValues(string entityName, string alias, Table criteria)
        {
            _crmContext.TableConverter.ConvertTable(entityName, criteria);
            _crmContext.CommandProcessor.Execute(new CreateRecordCommand(_crmContext, _seleniumContext, entityName, criteria, alias));
        }

        [Given(@"(.*) has the process stage (.*)")]
        [When(@"the process stage of (.*) is changed to (.*)")]
        public void SetProcessStage(string alias, string stageName)
        {
            _crmContext.CommandProcessor.Execute(new MoveToBusinessProcessStageCommand(_crmContext, alias, stageName));
        }

        [Given(@"a related ([^\s]+) from (.*) named (.*) with the following values")]
        [When(@"a related ([^\s]+) from (.*) named (.*) is created with the following values")]
        public void GivenRelatedEntityWithValues(string entityName, string parentAlias, string childAlias, Table criteria)
        {
            _crmContext.TableConverter.ConvertTable(entityName, criteria);
            _crmContext.CommandProcessor.Execute(new CreateRelatedRecordCommand(_crmContext, _seleniumContext, entityName, criteria, childAlias, parentAlias));
        }

        #endregion

        #region When

        [When(@"(.*) is moved to the next process stage")]
        public void MoveToNextStage(string alias)
        {
            _crmContext.CommandProcessor.Execute(new MoveToNextBusinessProcessStageCommand(_crmContext, alias));
        }

        [When(@"(.*) is updated with the following values")]
        public void WhenAliasIsUpdated(string alias, Table criteria)
        {
            EntityReference aliasRef = _crmContext.RecordCache[alias];
            _crmContext.TableConverter.ConvertTable(aliasRef.LogicalName, criteria);
            _crmContext.CommandProcessor.Execute(new UpdateRecordCommand(_crmContext, _seleniumContext, aliasRef, criteria));
        }

        [When(@"all asynchronous processes for (.*) are finished")]
        public void WhenAsyncJobsFinished(string alias)
        {
            _crmContext.CommandProcessor.Execute(new WaitForAsyncJobsCommand(_crmContext, alias));
        }

        [When(@"the status of (.*) is changed to (.*)")]
        public void WhenStatusChanges(string alias, string newStatus)
        {
            _crmContext.CommandProcessor.Execute(new UpdateStatusCommand(_crmContext, alias, newStatus));
        }

        [When(@"(.*) is deleted")]
        public void WhenAliasIsDeleted(string alias)
        {
            _crmContext.CommandProcessor.Execute(new DeleteRecordCommand(_crmContext, _seleniumContext, alias));
        }

        [When(@"(.*) is assigned to (.*)")]
        public void WhenAliasIsAssignedToAlias(string aliasToAssign, string aliasToAssignTo)
        {
            _crmContext.CommandProcessor.Execute(new AssignRecordCommand(_crmContext, aliasToAssign, aliasToAssignTo));
        }

        [When(@"(.*) is fully merged into (.*)")]
        public void WhenRecordsAreMerged(string subordindateAlias, string targetAlias)
        {
            var targetRecord = _crmContext.RecordCache.Get(targetAlias);
            var subordinateRecord = _crmContext.RecordCache.Get(subordindateAlias);
            _crmContext.CommandProcessor.Execute(new MergeRecordsCommand(_crmContext, targetRecord, subordinateRecord));
        }

        [When(@"The following fields of (.*) are fully merged into (.*)")]
        public void WhenRecordsAreMergedPartial(string subordindateAlias, string targetAlias, Table mergeTable)
        {
            var targetRecord = _crmContext.RecordCache.Get(targetAlias);
            var subordinateRecord = _crmContext.RecordCache.Get(subordindateAlias);
            _crmContext.TableConverter.ConvertTable(subordinateRecord.LogicalName, mergeTable);
            _crmContext.CommandProcessor.Execute(new MergeRecordsCommand(_crmContext, targetRecord, subordinateRecord, mergeTable));
        }

        [When(@"the following records of type ([^\s]+) are connected to (.*)")]
        public void AssociateRecordsViaNN(string relatedEntityName, string alias, Table records)
        {
            _crmContext.CommandProcessor.Execute(new AssociateToNNRelationshipCommand(_crmContext, alias, relatedEntityName, records));
        }


        #endregion

        #region Then


        [Then(@"the process stage of (.*) is (.*)")]
        public void AssertProcessStage(string alias, string stageName)
        {
            _crmContext.CommandProcessor.Execute(new AssertBusinessProcessStageCommand(_crmContext, alias, stageName));
        }

        [Then(@"(.*) has the following values")]
        public void ThenAliasHasValues(string alias, Table criteria)
        {
            EntityReference aliasRef = _crmContext.RecordCache[alias];
            _crmContext.TableConverter.ConvertTable(aliasRef.LogicalName, criteria);

            _crmContext.CommandProcessor.Execute(new AssertCrmRecordCommand(_crmContext, aliasRef, criteria));
        }

        [Then(@"a ([^\s]+) exists with the following values")]
        [Then(@"an ([^\s]+) exists with the following values")]
        public Entity ThenRecordExists(string entityName, Table criteria)
        {
            _crmContext.TableConverter.ConvertTable(entityName, criteria);
            DataCollection<Entity> records = _crmContext.CommandProcessor.Execute(new GetRecordsCommand(_crmContext, entityName, criteria));
            Assert.AreEqual(1, records.Count, string.Format("When looking for records for {0}, expected 1, but found {1} records", entityName, records.Count));

            return records[0];
        }

        [Then(@"a ([^\s]+) named (.*) exists with the following values")]
        [Then(@"an ([^\s]+) named (.*) exists with the following values")]
        public Entity ThenRecordExistsAndGiveAlias(string entityName, string alias, Table criteria)
        {
            _crmContext.TableConverter.ConvertTable(entityName, criteria);
            DataCollection<Entity> records = _crmContext.CommandProcessor.Execute(new GetRecordsCommand(_crmContext, entityName, criteria));
            Assert.AreEqual(1, records.Count, string.Format("When looking for records for {0}, expected 1, but found {1} records", entityName, records.Count));

            _crmContext.RecordCache.Add(alias, records[0]);

            return records[0];
        }

        [Then(@"(.*) has the following connected records of type ([^\s]+)")]
        public void ThenRecordsAreConnectedViaNN(string alias, string relatedEntityName, Table records)
        {
            _crmContext.CommandProcessor.Execute(new AssertNNRelationshipCommand(_crmContext, alias, relatedEntityName, records));
        }

        [Then(@"(.*) has the following form notifications")]
        public void ThenFormNotificationExist(string alias, Table formNotifications)
        {
            _crmContext.CommandProcessor.Execute(new AssertFormNotificationsCommand(_crmContext, _seleniumContext, alias, formNotifications));
        }

        #endregion


    }
}
