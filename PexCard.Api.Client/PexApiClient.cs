﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using PexCard.Api.Client.Const;
using PexCard.Api.Client.Core;
using PexCard.Api.Client.Core.Enums;
using PexCard.Api.Client.Core.Exceptions;
using PexCard.Api.Client.Core.Models;
using PexCard.Api.Client.Extensions;
using PexCard.Api.Client.Models;

namespace PexCard.Api.Client
{
    public class PexApiClient : IPexApiClient
    {
        private readonly HttpClient _httpClient;

        public PexApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Uri BaseUri => _httpClient.BaseAddress;

        public async Task<bool> Ping(CancellationToken token = default(CancellationToken))
        {
            const string url = "v4/ping";
            var response = await _httpClient.GetAsync(url, token);
            return response.IsSuccessStatusCode;
        }

        public async Task<RenewTokenResponseModel> RenewExternalToken(string externalToken,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.PostAsync("V4/Token/Renew", null, token);
            var result = await HandleHttpResponseMessage<RenewTokenResponseModel>(response);

            return result;
        }

        public async Task<string> ExchangeJwtForApiToken(string jwt, ExchangeTokenRequestModel exchangeTokenRequest,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Bearer, jwt);

            var content = new StringContent(JsonConvert.SerializeObject(exchangeTokenRequest), Encoding.UTF8,
                "application/json");

            var response =
                await _httpClient.PostAsync("Internal/V4/Account/Token/Exchange", content, cancellationToken);
            var result = await HandleHttpResponseMessage<string>(response);

            return result;
        }

        public async Task DeleteExternalToken(string externalToken, CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);
            var response = await _httpClient.DeleteAsync("V4/Token", token);

            await HandleHttpResponseMessage(response);
        }


        public async Task<decimal> GetPexAccountBalance(string externalToken, CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var builder = new UriBuilder(new Uri(_httpClient.BaseAddress, "V4/Business/Balance"));

            var response = await _httpClient.GetAsync(builder.Uri, token);
            var result = await HandleHttpResponseMessage<BusinessBalanceModel>(response);

            return result?.BusinessAccountBalance ?? 0;
        }

        public async Task<int> GetAllCardholderTransactionsCount(
            string externalToken,
            DateTime startDate,
            DateTime endDate,
            bool includePendings = false,
            bool includeDeclines = false,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var builder = new UriBuilder(new Uri(_httpClient.BaseAddress, "V4/Details/AllCardholderTransactionCount"));

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("IncludePendings", includePendings.ToString());
            query.Add("IncludeDeclines", includeDeclines.ToString());
            query.Add("StartDate", startDate.ToDateTimeString());
            query.Add("EndDate", endDate.ToDateTimeString());
            builder.Query = query.ToString();

            var response = await _httpClient.GetAsync(builder.Uri, token);
            var result = await HandleHttpResponseMessage<int>(response);

            return result;
        }

        public async Task<CardholderTransactions> GetAllCardholderTransactions(
            string externalToken,
            DateTime startDate,
            DateTime endDate,
            bool includePendings = false,
            bool includeDeclines = false,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var builder = new UriBuilder(new Uri(_httpClient.BaseAddress, "V4/Details/AllCardholderTransactions"));

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("IncludePendings", includePendings.ToString());
            query.Add("IncludeDeclines", includeDeclines.ToString());
            query.Add("StartDate", startDate.ToDateTimeString());
            query.Add("EndDate", endDate.ToDateTimeString());
            builder.Query = query.ToString();

            var response = await _httpClient.GetAsync(builder.Uri, token);
            var result = await HandleHttpResponseMessage<TransactionListModel>(response);

            return new CardholderTransactions(result?.TransactionList ?? new List<TransactionModel>());
        }

        public async Task<BusinessAccountTransactions> GetBusinessAccountTransactions(
            string externalToken,
            DateTime startDate,
            DateTime endDate,
            bool includePendings = false,
            bool includeDeclines = false,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var builder = new UriBuilder(new Uri(_httpClient.BaseAddress, "V4/Details/TransactionDetails"));

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("IncludePendings", includePendings.ToString());
            query.Add("IncludeDeclines", includeDeclines.ToString());
            query.Add("StartDate", startDate.ToDateTimeString());
            query.Add("EndDate", endDate.ToDateTimeString());
            builder.Query = query.ToString();

            var response = await _httpClient.GetAsync(builder.Uri, token);
            var result = await HandleHttpResponseMessage<TransactionListModel>(response);

            return new BusinessAccountTransactions(result?.TransactionList ?? new List<TransactionModel>());
        }

        public async Task<List<AttachmentLinkModel>> GetTransactionAttachments(string externalToken, long transactionId,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.GetAsync($"V4/Transactions/{transactionId}/Attachments", token);
            var result = await HandleHttpResponseMessage<AttachmentsModel>(response, true);

            return result?.Attachments;
        }

        public async Task<AttachmentModel> GetTransactionAttachment(string externalToken, long transactionId,
            string attachmentId, CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response =
                await _httpClient.GetAsync($"V4/Transactions/{transactionId}/Attachment/{attachmentId}", token);
            var result = await HandleHttpResponseMessage<AttachmentModel>(response, true);

            return result;
        }

        public async Task AddTransactionNote(string externalToken, TransactionModel transaction,
            string noteText, CancellationToken token = default(CancellationToken))
        {
            var noteRequest = new NoteRequestModel
            {
                NoteText = noteText,
                Pending = transaction.IsPending,
                TransactionId = transaction.TransactionId
            };

            const string url = "v4/note";
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);
            var content = new StringContent(JsonConvert.SerializeObject(noteRequest), Encoding.UTF8,
                "application/json");
            var response = await _httpClient.PostAsync(url, content, token);

            await HandleHttpResponseMessage(response);
        }
        

        public async Task<bool> IsTagsEnabled(string externalToken,
            CancellationToken token = default(CancellationToken))
        {
            var response = await GetTagsResponse(externalToken, token);
            await HandleHttpResponseMessageError(response);
            var result = response.StatusCode != HttpStatusCode.Forbidden;
            return result;
        }

        public async Task<bool> IsTagsAvailable(string externalToken, CustomFieldType fieldType,
            CancellationToken token = default(CancellationToken))
        {
            var response = await GetTagsResponse(externalToken, token);
            var responseContent = await HandleHttpResponseMessageError(response);

            if (response.StatusCode == HttpStatusCode.Forbidden) return false;

            var tags = JsonConvert.DeserializeObject<List<TagDetailsModel>>(responseContent);
            var result = tags.Any(x => x.Type == fieldType);
            return result;
        }

        /// <summary>
        /// Return all accounts associated with your business.
        /// </summary>
        public async Task<BusinessDetailsModel> GetBusinessDetails(string externalToken,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.GetAsync("V4/Details/AccountDetails", token);
            var result = await HandleHttpResponseMessage<BusinessDetailsModel>(response);

            return result;
        }

        /// <summary>
        /// Creates a card funding transaction. This transfers money from the business to the card making funds immediately available to spend.
        /// Retrieves the latest transactions and attaches a note to the latest funding transaction matching the requested amount. 
        /// </summary>
        public async Task<FundResponseModel> FundCard(string externalToken, int cardholderAccountId, decimal amount, string note,
            CancellationToken token = default(CancellationToken))
        {
            var fundingResult = await FundCard(externalToken, cardholderAccountId, amount, token);
            if (token.IsCancellationRequested)
            {
                return fundingResult;
            }
            var startDate = DateTime.UtcNow.AddSeconds(-30);
            startDate = new DateTime(
                startDate.Year,
                startDate.Month,
                startDate.Day,
                startDate.Hour,
                startDate.Minute,
                startDate.Second,
                DateTimeKind.Utc
            );

            var endDate = startDate.AddMinutes(1);

            var transactions = await GetCardholderTransactions(externalToken, cardholderAccountId, startDate, endDate, false,
                false, token);

            var tran = transactions.FindAll(
                    x => x.TransactionTypeCategory == "CardFunding" && Convert.ToDecimal(x.TransactionAmount) == amount)
                .OrderByDescending(x => x.TransactionTime)
                .First();

            if (token.IsCancellationRequested)
            {
                return fundingResult;
            }
            await AddTransactionNote(externalToken, tran, note, token);

            return fundingResult;
        }

        /// <summary>
        /// Creates a card funding transaction. This transfers money from the business to the card making funds immediately available to spend.
        /// </summary>
        public async Task<FundResponseModel> FundCard(string externalToken, int cardholderAccountId, decimal amount,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var requestContent = JsonConvert.SerializeObject(new FundRequestModel {Amount = amount});
            var request = new StringContent(requestContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"V4/Card/Fund/{cardholderAccountId}", request, token);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<FundResponseModel>(responseContent);

            return result;
        }

        public async Task<CardholderTransactions> GetCardholderTransactions(
            string externalToken,
            int cardholderAccountId,
            DateTime startDate,
            DateTime endDate,
            bool includePending = false,
            bool includeDeclines = false,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var builder = new UriBuilder(new Uri(_httpClient.BaseAddress, $"V4/Details/TransactionDetails/{cardholderAccountId}"));

            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("IncludePendings", includePending.ToString());
            query.Add("IncludeDeclines", includeDeclines.ToString());
            query.Add("StartDate", startDate.ToEst().ToDateTimeString());
            query.Add("EndDate", endDate.ToEst().ToDateTimeString());
            builder.Query = query.ToString();

            var response = await _httpClient.GetAsync(builder.Uri, token);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TransactionListModel>(responseContent);

            return new CardholderTransactions(result.TransactionList ?? new List<TransactionModel>());
        }


        public async Task<CardholderDetailsModel> GetCardholderDetails(string externalToken,
            int cardholderAccountId, CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.GetAsync($"V4/Details/AccountDetails/{cardholderAccountId}", token);
            var result = await HandleHttpResponseMessage<CardholderDetailsModel>(response);

            return result;
        }

        public async Task<CardholderProfileModel> GetCardholderProfile(string externalToken,
            int cardholderAccountId, CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.GetAsync($"V4/Card/Profile/{cardholderAccountId}", token);
            var result = await HandleHttpResponseMessage<CardholderProfileModel>(response);

            return result;
        }
        

        public async Task<List<TagDetailsModel>> GetTags(string externalToken, CancellationToken token = default(CancellationToken))
        {
            var response = await GetTagsResponse(externalToken, token);
            var result = await HandleHttpResponseMessage<List<TagDetailsModel>>(response);

            return result;
        }

        public async Task<TagDetailsModel> GetTag(string externalToken, string tagId,
            CancellationToken token = default(CancellationToken))
        {
            var result = await GetTag<TagDetailsModel>(externalToken, tagId, token);
            return result;
        }

        public async Task<TagDropdownDetailsModel> GetDropdownTag(string externalToken, string tagId,
            CancellationToken token = default(CancellationToken))
        {
            var result = await GetTag<TagDropdownDetailsModel>(externalToken, tagId, token);
            return result;
        }

        public async Task<TagDropdownDetailsModel> CreateDropdownTag(string externalToken, TagDropdownDataModel tag,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var requestContent = JsonConvert.SerializeObject(tag);
            var request = new StringContent(requestContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("V4/Business/Configuration/Tag/Dropdown", request, token);
            var result = await HandleHttpResponseMessage<TagDropdownDetailsModel>(response);

            return result;
        }

        public async Task<TagDropdownDetailsModel> UpdateDropdownTag(string externalToken, string tagId, TagDropdownModel tag,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var requestContent = JsonConvert.SerializeObject(tag);
            var request = new StringContent(requestContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"V4/Business/Configuration/Tag/Dropdown/{tagId}", request, token);
            var result = await HandleHttpResponseMessage<TagDropdownDetailsModel>(response);

            return result;
        }

        public async Task<TagDropdownDetailsModel> DeleteDropdownTag(string externalToken, string tagId,
            CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.DeleteAsync($"V4/Business/Configuration/Tag/Dropdown/{tagId}", token);
            var result = await HandleHttpResponseMessage<TagDropdownDetailsModel>(response);

            return result;
        }

        public async Task<TokenResponseModel> GetTokens(string externalToken, CancellationToken token = default(CancellationToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.GetAsync("V4/Token", token);
            var result = await HandleHttpResponseMessage<TokenResponseModel>(response);
            return result;
        }

        #region Private methods

        private async Task<HttpResponseMessage> GetTagsResponse(string externalToken, CancellationToken token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var result = await _httpClient.GetAsync("V4/Business/Configuration/Tags", token);
            return result;
        }

        private async Task<T> GetTag<T>(string externalToken, string tagId, CancellationToken token) where T : TagDataModel
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TokenType.Token, externalToken);

            var response = await _httpClient.GetAsync($"V4/Business/Configuration/Tag/{tagId}", token);
            var result = await HandleHttpResponseMessage<T>(response);

            return result;
        }

        private async Task<T> HandleHttpResponseMessage<T>(HttpResponseMessage response, bool notFoundAsDefault = false)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound && notFoundAsDefault)
                {
                    return default(T);
                }
                throw new PexApiClientException(response.StatusCode, responseContent);
            }
            var result = JsonConvert.DeserializeObject<T>(responseContent);
            return result;
        }

        private async Task HandleHttpResponseMessage(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new PexApiClientException(response.StatusCode, responseContent);
            }
        }

        private async Task<string> HandleHttpResponseMessageError(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            if ((int) response.StatusCode >= (int) HttpStatusCode.InternalServerError)
            {
                throw new PexApiClientException(response.StatusCode, responseContent);
            }
            return responseContent;
        }

        #endregion
    }
}
