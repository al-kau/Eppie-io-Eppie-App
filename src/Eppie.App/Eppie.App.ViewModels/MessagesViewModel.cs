﻿// ---------------------------------------------------------------------------- //
//                                                                              //
//   Copyright 2025 Eppie (https://eppie.io)                                    //
//                                                                              //
//   Licensed under the Apache License, Version 2.0 (the "License"),            //
//   you may not use this file except in compliance with the License.           //
//   You may obtain a copy of the License at                                    //
//                                                                              //
//       http://www.apache.org/licenses/LICENSE-2.0                             //
//                                                                              //
//   Unless required by applicable law or agreed to in writing, software        //
//   distributed under the License is distributed on an "AS IS" BASIS,          //
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.   //
//   See the License for the specific language governing permissions and        //
//   limitations under the License.                                             //
//                                                                              //
// ---------------------------------------------------------------------------- //

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Tuvi.App.ViewModels.Common;
using Tuvi.App.ViewModels.Services;
using Tuvi.Core.Entities;

namespace Tuvi.App.ViewModels
{
    public class MessagesViewModel : BaseViewModel, IIncrementalSource<MessageInfo>
    {
        public class BaseNavigationData
        {
            public IErrorHandler ErrorHandler { get; set; }
        }

        private IErrorHandler PageErrorHandler { get; set; }

        /// <summary>
        /// Time in milliseconds
        /// </summary>
        private static int _notificationShowDuration = 3000;

        private int _runningDeleteTasksCount;
        private int RunningDeleteTasksCount
        {
            get => _runningDeleteTasksCount;
            set
            {
                SetProperty(ref _runningDeleteTasksCount, value);
                OnPropertyChanged(nameof(IsDeleteInProcess));
            }
        }
        public bool IsDeleteInProcess
        {
            get => RunningDeleteTasksCount > 0;
        }

        private string _messagesDeletedText;
        public string MessagesDeletedText
        {
            get => _messagesDeletedText;
            set => SetProperty(ref _messagesDeletedText, value);
        }

        public IRelayCommand RefreshMessagesCommand { get; }

        public IRelayCommand OpenMessageCommand { get; }

        public IRelayCommand MarkSelectedMessagesAsReadCommand { get; }
        public IRelayCommand MarkMessageAsReadCommand { get; }

        public IRelayCommand MarkSelectedMessagesAsUnreadCommand { get; }
        public IRelayCommand MarkMessageAsUnreadCommand { get; }

        public IRelayCommand FlagSelectedMessagesCommand { get; }
        public IRelayCommand FlagMessageCommand { get; }

        public IRelayCommand UnflagSelectedMessagesCommand { get; }
        public IRelayCommand UnflagMessageCommand { get; }

        public IRelayCommand DeleteSelectedMessagesCommand { get; }
        public IRelayCommand DeleteMessageCommand { get; }

        public ICommand SelectedItemsChangedCommand => new RelayCommand(OnSelectedItemsChanged);

        public ICommand CancelMessagesDeleteCommand => new RelayCommand(CancelMessagesDelete);

        public ICommand StartDragMessagesCommand => new RelayCommand<IList<object>>(StartDragMessages);


        private ManagedCollection<MessageInfo> _messageList;
        public ManagedCollection<MessageInfo> MessageList
        {
            get { return _messageList; }
            set
            {
                if (_messageList is INotifyPropertyChanged oldList)
                {
                    oldList.PropertyChanged -= OnMessageListPropertyChanged;
                }

                SetProperty(ref _messageList, value);

                if (_messageList is INotifyPropertyChanged newList)
                {
                    newList.PropertyChanged += OnMessageListPropertyChanged;
                }
            }
        }

        private Message LastMessage { get; set; }

        private CancellationTokenSource _cancellationTokenSource;
        public CancellationTokenSource CancellationTokenSource { get { return _cancellationTokenSource; } }

        private void OnMessageListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ManagedCollection<MessageInfo>.IsChanging))
            {
                RefreshMessagesCommand.NotifyCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(ManagedCollection<MessageInfo>.ItemsFilter))
            {
                SaveSelectedItemsFilter(MessageList.ItemsFilter.GetType().Name);
            }
        }

        private bool _isSelectMessagesMode;
        public bool IsSelectMessagesMode
        {
            get { return _isSelectMessagesMode; }
            set
            {
                SetProperty(ref _isSelectMessagesMode, value);
                OpenMessageCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsLocalAIAvailable));
                UpdateAIButton();
            }
        }

        private bool _isLocalAIEnabled;
        public bool IsLocalAIEnabled
        {
            get => _isLocalAIEnabled;
            private set => SetProperty(ref _isLocalAIEnabled, value);
        }

        public bool IsLocalAIAvailable => IsSelectMessagesMode && AIService.IsAvailable();

        public MessageFilter[] FilterVariants { get; }

        protected IComparer<MessageInfo> MessageComparer => MessageList.ItemsComparer;

        public IFilter<MessageInfo> Filter => MessageList.ItemsFilter;

        public MessagesViewModel()
        {
            RefreshMessagesCommand = new AsyncRelayCommand(RefreshMessagesAsync, CanRefreshMessages);
            OpenMessageCommand = new RelayCommand<object>(OpenMessage, parameter => !IsSelectMessagesMode);
            MarkSelectedMessagesAsReadCommand = new AsyncRelayCommand<IList<object>>(MarkMessagesAsReadAsync, list => list != null && list.Any(item => item is MessageInfo message && !message.IsMarkedAsRead));
            MarkMessageAsReadCommand = new AsyncRelayCommand<MessageInfo>(MarkMessageAsReadAsync);
            MarkSelectedMessagesAsUnreadCommand = new AsyncRelayCommand<IList<object>>(MarkMessagesAsUnreadAsync, list => list != null && list.Any(item => item is MessageInfo message && message.IsMarkedAsRead));
            MarkMessageAsUnreadCommand = new AsyncRelayCommand<MessageInfo>(MarkMessageAsUnreadAsync);
            DeleteSelectedMessagesCommand = new AsyncRelayCommand<IList<object>>(DeleteMessagesAsync, list => list?.Count > 0, AsyncRelayCommandOptions.AllowConcurrentExecutions);
            DeleteMessageCommand = new AsyncRelayCommand<MessageInfo>(DeleteMessageAsync, AsyncRelayCommandOptions.AllowConcurrentExecutions);
            FlagSelectedMessagesCommand = new AsyncRelayCommand<IList<object>>(FlagMessagesAsync, list => list != null && list.Any(item => item is MessageInfo message && !message.IsFlagged));
            FlagMessageCommand = new AsyncRelayCommand<MessageInfo>(FlagMessageAsync);
            UnflagSelectedMessagesCommand = new AsyncRelayCommand<IList<object>>(UnflagMessagesAsync, list => list != null && list.Any(item => item is MessageInfo message && message.IsFlagged));
            UnflagMessageCommand = new AsyncRelayCommand<MessageInfo>(UnflagMessageAsync);
        }

        private bool CanRefreshMessages() => MessageList != null && !MessageList.IsChanging;

        public MessageFilter[] GetFilterVariants()
        {
            return new MessageFilter[]
            {
                new AllMessagesFilter(GetLocalizedString("AllMessagesFilterLabel")),
                new UnreadMessagesFilter(GetLocalizedString("UnreadMessagesFilterLabel")),
                new FlaggedMessagesFilter(GetLocalizedString("FlaggedMessagesFilterLabel")),
                new MessagesWithAttachmentsFilter(GetLocalizedString("MessagesWithAttachmentFilterLabel"))
            };
        }

        public override void OnNavigatedTo(object data)
        {
            if (data is BaseNavigationData navigationData)
            {
                PageErrorHandler = navigationData.ErrorHandler;
            }

            Debug.Assert(_cancellationTokenSource is null);
            _cancellationTokenSource = new CancellationTokenSource();
            SubscribeOnCoreEvents();

            UpdateAIButton();

            base.OnNavigatedTo(data);
        }

        public override void OnNavigatedFrom()
        {
            UnsubscribeFromCoreEvents();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            MessageList = null;
            Reset();
        }

        private async void UpdateAIButton()
        {
            try
            {
                IsLocalAIEnabled = await AIService.IsEnabledAsync().ConfigureAwait(true);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        protected virtual void SubscribeOnCoreEvents()
        {
            Core.MessagesReceived += OnMessagesReceived;
            Core.MessageDeleted += OnMessageDeleted;
            Core.MessagesIsReadChanged += OnMessagesAttributeChanged;
            Core.MessagesIsFlaggedChanged += OnMessagesAttributeChanged;
        }

        protected virtual void UnsubscribeFromCoreEvents()
        {
            Core.MessagesReceived -= OnMessagesReceived;
            Core.MessageDeleted -= OnMessageDeleted;
            Core.MessagesIsReadChanged -= OnMessagesAttributeChanged;
            Core.MessagesIsFlaggedChanged -= OnMessagesAttributeChanged;
        }

        public override void OnError(Exception e)
        {
            PageErrorHandler?.OnError(e, false);
        }

        private async void OnMessagesReceived(object sender, MessagesReceivedEventArgs e)
        {
            try
            {
                var messageList = MessageList;
                if (messageList is null)
                {
                    return;
                }

                await DispatcherService.RunAsync(() =>
                {
                    var messages = SelectAppropriateMessagesFrom(e.ReceivedMessages);
                    messageList?.AddRange(messages);
                }).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        protected virtual IEnumerable<MessageInfo> SelectAppropriateMessagesFrom(List<ReceivedMessageInfo> receivedMessages)
        {
            return receivedMessages.Select(m => new MessageInfo(m.Email, m.Message));
        }

        private async void OnMessageDeleted(object sender, MessageDeletedEventArgs e)
        {
            try
            {
                var messageList = MessageList;
                if (messageList is null)
                {
                    return;
                }
                await DispatcherService.RunAsync(() =>
                {
                    var deletedMessage = messageList.FirstOrDefault(m => m.MessageID == e.MessageID && m.Email.HasSameAddress(e.Email) && m.Folder.HasSameName(e.Folder));

                    if (deletedMessage != null)
                    {
                        messageList.Remove(deletedMessage);
                    }
                }).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }
        private async void OnMessagesAttributeChanged(object sender, MessagesAttributeChangedEventArgs e)
        {
            try
            {
                var messageList = MessageList;
                if (messageList is null)
                {
                    return;
                }
                await DispatcherService.RunAsync(() =>
                {
                    var changedMessages = messageList.OriginalItems.Join(e.Messages,
                                                                         message => message.MessageID,
                                                                         changedMessage => changedMessage.Id,
                                                                         (message, id) => message)
                                                                    .Zip(e.Messages,
                                                                        (mi, m) =>
                                                                        {
                                                                            mi.UpdateMessageAttributes(m);
                                                                            return mi;
                                                                        })
                                                                    .ToArray();

                    messageList.VerifyItemsPassFilter(changedMessages);

                }).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public async Task<IEnumerable<MessageInfo>> LoadMoreItemsAsync(int count, CancellationToken cancellationToken)
        {
            var lastMessage = LastMessage;
            var messageComparer = MessageComparer;
            var loadedMessages = await LoadMoreMessagesAsync(count, lastMessage, cancellationToken).ConfigureAwait(true);
            if (loadedMessages.Count > 0)
            {
                LastMessage = loadedMessages[loadedMessages.Count - 1];
            }

            // HACK: this is the most sutable and easy way to let know that there are no more items, without breaking tests and changing a bunch of interfaces
            if (loadedMessages.Count == 1 && loadedMessages[0] == null)
            {
                return new List<MessageInfo>() { null };
            }

            var messages = loadedMessages.Select(m => new MessageInfo(m.Folder.AccountEmail, m));
            if (messageComparer is null)
            {
                return messages.OrderByDescending(mi => mi.MessageData.Date);
            }
            return messages.OrderBy(mi => mi, messageComparer);
        }

        protected virtual Task<IReadOnlyList<Message>> LoadMoreMessagesAsync(int count, Message lastMessage, CancellationToken cancellationToken)
        {
            return Task.FromResult(Array.Empty<Message>() as IReadOnlyList<Message>);
        }

        public void Reset()
        {
            LastMessage = null;
        }

        protected virtual async Task RefreshMessagesAsync()
        {
            if (MessageList is null)
            {
                // we leaved the page
                return;
            }
            await MessageList.RefreshAsync().ConfigureAwait(true);
        }

        private async Task<MessageInfo> GetMessageBodyAsync(MessageInfo messageInfo)
        {
            messageInfo.MessageData = await Core.GetMessageBodyHighPriorityAsync(messageInfo.MessageData).ConfigureAwait(true);

            return messageInfo;
        }

        protected virtual void OpenMessage(object parameter)
        {
            if (parameter is MessageInfo messageInfo)
            {
                if (messageInfo.Folder.IsDraft)
                {
                    NavigationService?.Navigate(nameof(NewMessagePageViewModel), new DraftMessageData(messageInfo, Core.GetTextUtils(), messageInfo.IsEmptyBody ? GetMessageBodyAsync(messageInfo) : null));
                }
                else
                {
                    NavigationService?.Navigate(nameof(MessagePageViewModel), messageInfo);
                }
            }
        }

        private Task MarkMessageAsReadAsync(MessageInfo parameter, CancellationToken cancellationToken = default)
        {
            return MarkMessagesAsReadAsync(new List<object> { parameter }, cancellationToken);
        }

        private Task MarkMessagesAsReadAsync(IList<object> parameter, CancellationToken cancellationToken = default)
        {
            return ApplyMessageCommandAsync(parameter,
                                            m => !m.IsMarkedAsRead,
                                            m => m.MarkAsRead(),
                                            Core.MarkMessagesAsReadAsync,
                                            cancellationToken);
        }

        private Task MarkMessageAsUnreadAsync(MessageInfo parameter, CancellationToken cancellationToken = default)
        {
            return MarkMessagesAsUnreadAsync(new List<object> { parameter }, cancellationToken);
        }

        private Task MarkMessagesAsUnreadAsync(IList<object> parameter, CancellationToken cancellationToken = default)
        {
            return ApplyMessageCommandAsync(parameter,
                                            m => m.IsMarkedAsRead,
                                            m => m.MarkAsUnread(),
                                            Core.MarkMessagesAsUnReadAsync,
                                            cancellationToken);
        }

        private Task FlagMessageAsync(MessageInfo parameter, CancellationToken cancellationToken = default)
        {
            return FlagMessagesAsync(new List<object> { parameter }, cancellationToken);
        }

        private Task FlagMessagesAsync(IList<object> parameter, CancellationToken cancellationToken = default)
        {
            return ApplyMessageCommandAsync(parameter,
                                            m => !m.IsFlagged,
                                            m => m.Flag(),
                                            Core.FlagMessagesAsync,
                                            cancellationToken);
        }

        private Task UnflagMessageAsync(MessageInfo parameter, CancellationToken cancellationToken = default)
        {
            return UnflagMessagesAsync(new List<object> { parameter }, cancellationToken);
        }

        private Task UnflagMessagesAsync(IList<object> parameter, CancellationToken cancellationToken = default)
        {
            return ApplyMessageCommandAsync(parameter,
                                            m => m.IsFlagged,
                                            m => m.Unflag(),
                                            Core.UnflagMessagesAsync,
                                            cancellationToken);
        }

        private async Task ApplyMessageCommandAsync(IList<object> parameter,
                                                    Func<MessageInfo, bool> filter,
                                                    Action<MessageInfo> quickAction,
                                                    Func<IEnumerable<Message>, CancellationToken, Task> command,
                                                    CancellationToken cancellationToken)
        {
            try
            {
                var messages = parameter.Where(item => item is MessageInfo messageInfo && filter(messageInfo))
                                        .Select(item => item as MessageInfo).ToArray();

                // For quick response in the UI (immediately displays changes, without waiting for the server to change the flag for each letter)
                foreach (var messageInfo in messages)
                {
                    quickAction(messageInfo);
                }
                MessageList.VerifyItemsPassFilter(messages);

                // Remove the selection from the mail list
                parameter.Clear();

                await command(messages.Select(x => x.MessageData), cancellationToken).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        private Task DeleteMessageAsync(MessageInfo parameter, CancellationToken cancellationToken = default)
        {
            return DeleteMessagesAsync(new List<object> { parameter }, cancellationToken);
        }

        CancellationTokenSource _lastDeletedTokenSource;
        private async Task DeleteMessagesAsync(IList<object> parameter, CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = parameter
                    .OfType<MessageInfo>()
                    .ToArray();

                if (messages.Length == 0)
                {
                    return;
                }

                foreach (var message in messages)
                {
                    message.WasDeleted = true;
                }
                var messageList = MessageList;
                messageList.VerifyItemsPassFilter(messages);

                SetUpMessagesDeletedText(messages.Length);

                var delayTokenSource = new CancellationTokenSource();
                // We can cancel only last deleted message(-s),
                // even if previous deletion still can be canceled
                _lastDeletedTokenSource = delayTokenSource;

                bool processDeleteWasCanceled = false;
                try
                {
                    RunningDeleteTasksCount++;
                    await Task.Delay(_notificationShowDuration, delayTokenSource.Token).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    processDeleteWasCanceled = true;
                }
                finally
                {
                    RunningDeleteTasksCount--;
                    delayTokenSource.Dispose();
                }

                if (processDeleteWasCanceled)
                {
                    foreach (var message in messages)
                    {
                        message.WasDeleted = false;
                    }
                    messageList.VerifyItemsPassFilter(messages);
                }
                else
                {
                    await Core.DeleteMessagesAsync(messages.Select(x => x.MessageData).ToList(), CancellationToken.None).ConfigureAwait(true);

                    foreach (var messageInfo in messages)
                    {
                        messageList.Remove(messageInfo);
                    }
                }
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        private void SetUpMessagesDeletedText(int messagesCount)
        {
            MessagesDeletedText = (messagesCount == 1)
                ? GetLocalizedString("MessageWasDeleted")
                : string.Format(CultureInfo.CurrentCulture, GetLocalizedString("NumberOfMessagesWereDeleted"), messagesCount);
        }

        private void OnSelectedItemsChanged()
        {
            MarkSelectedMessagesAsReadCommand.NotifyCanExecuteChanged();
            MarkSelectedMessagesAsUnreadCommand.NotifyCanExecuteChanged();
            FlagSelectedMessagesCommand.NotifyCanExecuteChanged();
            UnflagSelectedMessagesCommand.NotifyCanExecuteChanged();
            DeleteSelectedMessagesCommand.NotifyCanExecuteChanged();
        }

        private void CancelMessagesDelete()
        {
            try
            {
                _lastDeletedTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // If token source was disposed, do nothing
            }
        }

        private void StartDragMessages(IList<object> parameter)
        {
            DragAndDropService.SetDraggedMessages(parameter.Select(x => x as MessageInfo).ToList());
        }

        public MessageFilter GetSavedSelectedFilter(MessageFilter[] filterVariants)
        {
            string selectedFilterName = GetSavedSelectedFilter();
            var selectedFilter = filterVariants.FirstOrDefault(filter => filter.GetType().Name == selectedFilterName);
            return selectedFilter ?? filterVariants.OfType<AllMessagesFilter>().FirstOrDefault();
        }

        virtual protected string GetSavedSelectedFilter()
        {
            throw new NotImplementedException();
        }

        virtual public void SaveSelectedItemsFilter(string filter)
        {
            throw new NotImplementedException();
        }

        public override async Task CreateAIAgentsMenuAsync(Action<string, Action<IList<object>>> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var agents = await AIService.GetAgentsAsync().ConfigureAwait(true);
            foreach (var agent in agents)
            {
                action(agent.Name, (items) => ProcessMessages(agent, items));
            }
        }

        private async Task AIAgentProcessMessagesAsync(LocalAIAgent agent, IReadOnlyList<MessageInfo> messages)
        {
            foreach (var message in messages)
            {
                message.MessageData = await Core.GetMessageBodyHighPriorityAsync(message.MessageData).ConfigureAwait(true);

                await AIAgentProcessMessageAsync(agent, message).ConfigureAwait(true);
            }
        }

        private async void ProcessMessages(LocalAIAgent agent, IList<object> items)
        {
            try
            {
                var messages = items.Select(x => x as MessageInfo).ToList();
                await AIAgentProcessMessagesAsync(agent, messages).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }
    }
}
