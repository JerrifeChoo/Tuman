<h6>$${\color{red}提示：这个仓库可能会做一写乱七八糟的东西}$$</h6>
<h1>基于Dots/ECS的Timer</h1>包含支持Burst的主系统和销毁回调的托管系统。
<h2>TimerSystem</h2>计时器主系统，包含三种执行方式主线程、单线程、多线程并发，根据工作线程数、当前实体数、Chunk数实时调整。
<h2>HandleSystem</h2>将计时器回调、销毁等设计托管数据内容的单独放在这个系统。
<h2>TimerBridge</h2>做为Mono与计时器系统交互的桥梁，包含创建、销毁、暂停、继续等接口
<h3>效果</h3>


https://github.com/user-attachments/assets/c84fb378-8b4c-4633-a6d7-f09efc9cb20d


<img width="1243" height="892" alt="image" src="https://github.com/user-attachments/assets/1c156d72-7a04-4463-b6e0-c9ebcc3d2b48" />
<h3>主线程</h3>
<img width="1503" height="607" alt="image" src="https://github.com/user-attachments/assets/744431f0-3b6e-4921-87af-00d8b5cb1468" /><br>
<img width="1504" height="928" alt="image" src="https://github.com/user-attachments/assets/b9d3bbb7-f6de-4226-8a48-006523823b8b" />
<h3>子线程</h3>
<img width="1506" height="956" alt="image" src="https://github.com/user-attachments/assets/6c004acd-7b6c-408a-a033-eb01d011eeea" />
<h3>多线程并发</h3>
<img width="1506" height="658" alt="image" src="https://github.com/user-attachments/assets/e8da323f-3289-4fd2-b12d-df3ee9b2c473" />
