//------------------------------------------------------------------------------
//  Copyright (c) 2014-2016 the original author or authors. All Rights Reserved.
//
//  NOTICE: You are permitted to use, modify, and distribute this file
//  in accordance with the terms of the license agreement accompanying it.
//------------------------------------------------------------------------------

using Robotlegs.Bender.Extensions.CommandCenter.API;
using Robotlegs.Bender.Extensions.CommandCenter.Impl;
using Robotlegs.Bender.Extensions.DirectAsyncCommand.API;
using Robotlegs.Bender.Framework.API;
using System;
using System.Collections.Generic;

namespace Robotlegs.Bender.Extensions.DirectAsyncCommand.Impl
{
    internal class AsyncCommandExecutor : IAsyncCommandExecutor
    {
        private const string AsyncCommandExecutedCallbackName = "AsyncCommandExecutedCallback";

        /*============================================================================*/
        /* Private Fields                                                         */
        /*============================================================================*/

        private ICommandExecutor _commandExecutor;
        private Queue<ICommandMapping> _commandMappingQueue;
        private int _totalCommandsCount;
        private Action _commandsAbortedCallback;
        private Action _commandsExecutedCallback;
        private Action<Type, int, int> _commandExecutedCallback;
        private IContext _context;

        private IAsyncCommand _currentAsyncCommand;
        private CommandExecutor.HandleResultDelegate _handleResult;
        private IInjector _injector;
        private CommandPayload _payload;

        /*============================================================================*/
        /* Constructors                                                           */
        /*============================================================================*/

        public AsyncCommandExecutor(IContext context, IInjector injector,
            CommandExecutor.RemoveMappingDelegate removeMapping = null, CommandExecutor.HandleResultDelegate handleResult = null)
        {
            IsAborted = false;
            _context = context;
            _handleResult = handleResult;
            _commandExecutor = new CommandExecutor(injector, removeMapping, HandleCommandExecuteResult, PreprocessAsyncCommandExecuting);
            _injector = _commandExecutor.Injector;
        }

        /*============================================================================*/
        /* Public Properties                                                         */
        /*============================================================================*/

        public bool IsAborted
        {
            get;
            private set;
        }

        /*============================================================================*/
        /* Public Functions                                                           */
        /*============================================================================*/

        /// <summary>
        /// Aborts asynchronous Command execution.
        /// </summary>
        /// <param name="abortCurrentCommand">if set to <c>true</c> abort current command execution.</param>
        public void Abort(bool abortCurrentCommand = true)
        {
            IsAborted = true;

            if (abortCurrentCommand && _currentAsyncCommand != null)
            {
                _currentAsyncCommand.Abort();
            }
        }

        public void ExecuteAsyncCommands(IEnumerable<ICommandMapping> mappings, CommandPayload payload)
        {
            _commandMappingQueue = new Queue<ICommandMapping>(mappings);
            _totalCommandsCount = _commandMappingQueue.Count;
            _payload = payload;
            ExecuteNextCommand();
        }

        public void SetCommandsAbortedCallback(Action callback)
        {
            _commandsAbortedCallback = callback;
        }

        public void SetCommandsExecutedCallback(Action callback)
        {
            _commandsExecutedCallback = callback;
        }

        public void SetCommandExecutedCallback(Action<Type, int, int> callback)
        {
            _commandExecutedCallback = callback;
        }

        /*============================================================================*/
        /* Private Functions                                                           */
        /*============================================================================*/

        private void CommandExecutedCallback(IAsyncCommand command, bool stop = false)
        {
            _context.Release(command);

            var current = _totalCommandsCount - _commandMappingQueue.Count;
            _commandExecutedCallback?.Invoke(command.GetType(), current, _totalCommandsCount);

            if (stop)
            {
                Abort(false);
            }

            ExecuteNextCommand();
        }

        private void ExecuteNextCommand()
        {
            if (_injector.HasMapping<Action<IAsyncCommand, bool>>(AsyncCommandExecutedCallbackName))
                _injector.Unmap<Action<IAsyncCommand, bool>>(AsyncCommandExecutedCallbackName);

            while (!IsAborted && _commandMappingQueue.Count > 0)
            {
                ICommandMapping mapping = _commandMappingQueue.Dequeue();

                if (mapping != null)
                {
                    _injector.Map<Action<IAsyncCommand, bool>>(AsyncCommandExecutedCallbackName).ToValue((Action<IAsyncCommand, bool>)CommandExecutedCallback);
                    _commandExecutor.ExecuteCommand(mapping, _payload);
                    return;
                }
            }

            if (IsAborted)
            {
                _commandMappingQueue.Clear();
                _commandsAbortedCallback?.Invoke();
            }
            else if (_commandMappingQueue.Count == 0)
            {
                _commandsExecutedCallback?.Invoke();
            }
        }

        private void PreprocessAsyncCommandExecuting(object command, ICommandMapping CommandMapping)
        {
            _currentAsyncCommand = command as IAsyncCommand;
            _context.Detain(_currentAsyncCommand);
        }

        private void HandleCommandExecuteResult(object result, object command, ICommandMapping CommandMapping)
        {
            if (_handleResult != null)
                _handleResult.Invoke(result, command, CommandMapping);
        }
    }
}