# tools/translate 入口说明

进入这个目录后，按下面顺序读取：

1. 先看 [MUST.md](C:/code/ETG-Gameplay-Dashboard/tools/translate/MUST.md)
   - 只放最短硬规则
   - 先确认边界和闭环

2. 再看 [README.md](C:/code/ETG-Gameplay-Dashboard/tools/translate/README.md)
   - 只看流程和命令
   - 不在这里硬记大量翻译细则

3. 需要正式翻译时，再看 [GLOSSARY.md](C:/code/ETG-Gameplay-Dashboard/tools/translate/GLOSSARY.md)
   - 用来统一术语和固定表达

4. 想理解这些 Python 为什么存在、分别负责什么时，再看 [SCRIPTS.md](C:/code/ETG-Gameplay-Dashboard/tools/translate/SCRIPTS.md)
   - 用来理解脚本职责
   - 用来理解为什么这里强调自动检查，而不是只靠文档规则

5. 需要执行脚本时，再按 README 里的命令调用：
   - `main.py`
     - 统一主命令入口
     - 日常优先使用 `main.py check` / `main.py normalize` / `main.py apply`
   - 只有在排查脚本行为时，再直接调用底层脚本

文件职责约定：

- `MUST.md`
  - 只放硬规则
- `README.md`
  - 只放流程
- `GLOSSARY.md`
  - 只放术语和翻译规则
- `SCRIPTS.md`
  - 只放脚本职责与设计原因
- `ENTRYPOINT.md`
  - 只放入口说明
