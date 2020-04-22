﻿using NTMiner.MinerStudio.Vms;
using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace NTMiner.MinerStudio {
    public static partial class MinerStudioRoot {
        public class MineWorkViewModels : ViewModelBase {
            public static readonly MineWorkViewModels Instance = new MineWorkViewModels();
            private readonly Dictionary<Guid, MineWorkViewModel> _dicById = new Dictionary<Guid, MineWorkViewModel>();
            public ICommand Add { get; private set; }

            private MineWorkViewModels() {
#if DEBUG
                NTStopwatch.Start();
#endif
                if (WpfUtil.IsInDesignMode) {
                    return;
                }
                foreach (var item in NTMinerContext.Instance.MinerStudioContext.MineWorkSet.AsEnumerable()) {
                    if (!_dicById.ContainsKey(item.Id)) {
                        _dicById.Add(item.Id, new MineWorkViewModel(item));
                    }
                }
                if (RpcRoot.IsOuterNet) {
                    AppRoot.AddEventPath<MineWorkSetInitedEvent>("作业集初始化后初始化Vm内存", LogEnum.DevConsole, action: message => {
                        foreach (var item in NTMinerContext.Instance.MinerStudioContext.MineWorkSet.AsEnumerable()) {
                            if (!_dicById.ContainsKey(item.Id)) {
                                _dicById.Add(item.Id, new MineWorkViewModel(item));
                            }
                        }
                        OnPropertyChangeds();
                        MinerClientsWindowViewModel.Instance.RefreshMinerClientsSelectedMineWork(MinerClientsWindowViewModel.Instance.MinerClients.ToArray());
                    }, this.GetType());
                }            
                this.Add = new DelegateCommand(() => {
                    new MineWorkViewModel(Guid.NewGuid()).Edit.Execute(FormType.Add);
                });
                AppRoot.AddEventPath<MineWorkAddedEvent>("添加作业后刷新VM内存", LogEnum.DevConsole,
                    action: message => {
                        if (!_dicById.TryGetValue(message.Source.GetId(), out MineWorkViewModel vm)) {
                            vm = new MineWorkViewModel(message.Source);
                            _dicById.Add(message.Source.GetId(), vm);
                            OnPropertyChangeds();
                            if (message.Source.GetId() == MinerClientsWindowVm.SelectedMineWork.GetId()) {
                                MinerClientsWindowVm.SelectedMineWork = MineWorkViewModel.PleaseSelect;
                            }
                        }
                    }, location: this.GetType());
                AppRoot.AddEventPath<MineWorkUpdatedEvent>("添加作业后刷新VM内存", LogEnum.DevConsole,
                    action: message => {
                        if (_dicById.TryGetValue(message.Source.GetId(), out MineWorkViewModel vm)) {
                            vm.Update(message.Source);
                        }
                    }, location: this.GetType());
                AppRoot.AddEventPath<MineWorkRemovedEvent>("移除了作业后刷新Vm内存", LogEnum.DevConsole, action: message => {
                    if (_dicById.TryGetValue(message.Source.Id, out MineWorkViewModel vm)) {
                        _dicById.Remove(vm.Id);
                        OnPropertyChangeds();
                        if (vm.Id == MinerClientsWindowVm.SelectedMineWork.GetId()) {
                            MinerClientsWindowVm.SelectedMineWork = MineWorkViewModel.PleaseSelect;
                        }
                    }
                }, this.GetType());
#if DEBUG
                var elapsedMilliseconds = NTStopwatch.Stop();
                if (elapsedMilliseconds.ElapsedMilliseconds > NTStopwatch.ElapsedMilliseconds) {
                    Write.DevTimeSpan($"耗时{elapsedMilliseconds} {this.GetType().Name}.ctor");
                }
#endif
            }

            private void OnPropertyChangeds() {
                OnPropertyChanged(nameof(List));
                OnPropertyChanged(nameof(MineWorkVmItems));
            }

            public List<MineWorkViewModel> List {
                get {
                    return _dicById.Values.ToList();
                }
            }

            private IEnumerable<MineWorkViewModel> GetMineWorkVmItems() {
                yield return MineWorkViewModel.PleaseSelect;
                foreach (var item in List) {
                    yield return item;
                }
            }

            public List<MineWorkViewModel> MineWorkVmItems {
                get {
                    return GetMineWorkVmItems().ToList();
                }
            }

            public bool TryGetMineWorkVm(Guid id, out MineWorkViewModel mineWorkVm) {
                return _dicById.TryGetValue(id, out mineWorkVm);
            }
        }
    }
}
