# ADR-003：SRS 算法采用 FSRS

- 状态：**已接受**
- 日期：2026-11-04

## 背景

需求 2 要求专用背单词模块实现智能记忆法。候选：SM-2（SuperMemo 2，Anki 经典算法）、FSRS（Free Spaced Repetition Scheduler，自由间隔重复调度器）、朴素的 Leitner 盒系统。

## 决策

采用 **FSRS** 作为唯一首发调度算法。调度卡以词元/词义为单位，按（`LearnerProfileId`, `LemmaId`）唯一；屈折形态掌握度独立记录，但经过判分且关联到形态的复习结果可用于所属词元卡评级。算法状态与参数版本必须持久化，以便升级时迁移或重算。

## 后果

- **优点**：支持以目标记忆率调参；目标记忆率按学习配置生效，首发默认值为 `0.9`。
- **缺点**：实现和状态迁移较 SM-2 复杂；参数与算法状态须版本化，并通过 `IFsrsParameterProvider` / `ISrsScheduler` 隔离。
- FSRS 类型仅存在于 Learning 模块；Vocabulary 模块只提供词元/形态身份，不暴露调度状态。
