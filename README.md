<h6>$${\color{red}提示：这个仓库可能会做一写乱七八糟的东西}$$</h6>

<h1>基于Dots/ECS的Timer</h1>
<h4>一、基于Dots/ECS实现的计时器系统。</h4>
<h4>二、包含主线程、单线程、多线程并行3种模式，运行时动态调度。</h4>
<h4>三、主系统支持BurstCompile，回调系统支持托管类型。</h4>
<h4>四、无SubScene子场景，动态创建EntityPrefab用于计时器实例化。</h4>
<h4>五、支持Mono，支持Time.timeScale缩放，支持暂停/恢复时长补偿。</h4>
<h4>六、基于Time.realtimeSinceStartup计算，比Time.deltaTime更精准，减少多次累加导致的精度丢失。</h4>
<h2>TimerSystem</h2>计时器主系统，包含三种执行方式主线程、单线程、多线程并发，根据工作线程数、当前实体数、Chunk数实时调整。
<h2>HandleSystem</h2>包含计时器回调、销毁回调涉及托管数据内容。
<h2>TimerBridge</h2>与Mono的交互桥梁。
<h4>添加计时器</h4>
<h4>$${\color{blue}TimerBridge.Add(float \space interval,HandleSystem.CallbackHandler \space onCallback,HandleSystem.CallbackHandler}$$</h4>
<h4>$${\color{blue}\space \space  \space onDestroy,int \space repeatCount=1, bool \space ignoreScale = false, bool \space ignoreGap = false)}$$</h4>
interval:间隔（0表示延迟一帧），onCallback：计时器回调函数，onDestroy：销毁回调函数，repeatCount：重复次数（-1表示无限循环），ignoreScale：忽略Tiem.timeScale时间缩放，ignoreGap：忽略暂停到恢复中间的时间损耗
<h4>移除计时器</h4>
<h4>$${\color{blue}TimerBridge.Remove(Entity \space entity)}$$ 移除单个</h4>
<h4>$${\color{blue}TimerBridge.RemoveAll()}$$ 移除所有</h4>
<h4>暂停计时器</h4>
<h4>$${\color{blue}TimerBridge.Pause(Entity \space entity}$$ 暂停单个</h4>
<h4>$${\color{blue}TimerBridge.PauseAll()}$$ 暂停所有</h4>
<h4>恢复计时器</h4>
<h4>$${\color{blue}TimerBridge.Resume(Entity \space entity)}$$ 恢复单个</h4>
<h4>$${\color{blue}TimerBridge.ResumeAll()}$$ 恢复所有</h4>
<h4>启用/禁用系统</h4>
<h4>$${\color{blue}TimerBridge.SetSystemEnabled(bool \space enabled)}$$ 禁用会停止计时，重新启用不会计算暂停补偿</h4>
<h4>$${\color{blue}TimerBridge.IsSystemEnabled()}$$返回系统状态</h4>
<h3>效果</h3>
<h4>支持Time.timeScale</h4>


https://github.com/user-attachments/assets/fe016a09-b106-4807-8e13-d06cbe140e60



https://github.com/user-attachments/assets/c84fb378-8b4c-4633-a6d7-f09efc9cb20d


<img width="1243" height="892" alt="image" src="https://github.com/user-attachments/assets/1c156d72-7a04-4463-b6e0-c9ebcc3d2b48" />
<h3>主线程</h3>
<img width="1503" height="607" alt="image" src="https://github.com/user-attachments/assets/744431f0-3b6e-4921-87af-00d8b5cb1468" /><br>
<img width="1504" height="928" alt="image" src="https://github.com/user-attachments/assets/b9d3bbb7-f6de-4226-8a48-006523823b8b" />
<h3>子线程</h3>
<img width="1506" height="956" alt="image" src="https://github.com/user-attachments/assets/6c004acd-7b6c-408a-a033-eb01d011eeea" />
<h3>多线程并发</h3>
<img width="1506" height="658" alt="image" src="https://github.com/user-attachments/assets/e8da323f-3289-4fd2-b12d-df3ee9b2c473" />
