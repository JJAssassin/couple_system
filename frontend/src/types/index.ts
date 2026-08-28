// 与后端 DTO 对齐（后端 camelCase 输出）
export interface ApiResult<T> {
  code: number;
  msg: string;
  data: T;
  success: boolean;
}
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// 后端 PermissionType / RoleType 枚举以数字序列化（1/2/3、1/2），与后端 Enums.cs 对齐
export type PermissionType = 1 | 2 | 3; // 1=公开 2=仅自己 3=对方可读
export type RoleType = 1 | 2; // 1=PartnerA 2=PartnerB

export interface UserProfile {
  id: number;
  nickName: string;
  avatar?: string;
  roleType: RoleType;
  loveStartTime: string;
}
export interface LoginResp {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  userProfile: UserProfile;
}
export interface LoveInfo {
  hasLoveStart: boolean;
  totalDays: number;
  totalHours: number;
  totalMinutes: number;
  loveStartTime: string;
}

// ---------- 情侣共享设置 ----------
export interface CoupleSetting {
  loveStartTime?: string | null;
  coupleName?: string | null;
  coupleAvatar?: string | null;
  lunarLoveStart?: string | null; // 相恋日的农历表示（审计 #14）
}

// ---------- 绑定对方 ----------
export interface PartnerInfo {
  id: number;
  nickName: string;
  avatar?: string | null;
  roleType: RoleType;
}
export interface BindStatus {
  isBound: boolean;
  partner: PartnerInfo | null;
  coupleId?: string | null;
  canInvite: boolean;
}
export interface InviteResp {
  code: string;
  expiresAt: string;
}
export interface ChartPoint {
  label: string;
  value: number;
}
export interface DashboardData {
  moodTrend: ChartPoint[];
  conflictTrend: ChartPoint[];
  wishCompleteRate: number;
  accountSummary: { income: number; expend: number; balance: number };
  activeStreakDays: number;
}
export interface AnniversaryDto {
  id: number;
  name: string;
  anniversaryType: number;
  targetDate: string;
  coverImage?: string;
  remindDays: number;
  daysLeft: number;
  isYearly: boolean;
  nextOccurrence?: string | null;
  lunarDate?: string | null; // 目标日的农历表示（审计 #14）
  createUserId: number;
  createTime: string;
}
export interface AnniversaryReq {
  name: string;
  anniversaryType: number;
  targetDate: string;
  coverImage?: string;
  remindDays: number;
  isYearly: boolean;
}

// ---------- 日记 ----------
export interface DiaryDto {
  id: number;
  title: string;
  content: string;
  moodTag?: string;
  moodScore: number;
  permissionType: PermissionType;
  weather?: string;
  diaryDate?: string;
  createUserId: number;
  createTime: string;
}
export interface DiaryReq {
  title: string;
  content: string;
  moodTag?: string;
  moodScore: number;
  permissionType: PermissionType;
  weather?: string;
  diaryDate?: string;
}
export interface DiaryCommentDto {
  id: number;
  diaryId: number;
  content: string;
  createUserId: number;
  createTime: string;
}
export interface DiaryCommentReq {
  diaryId: number;
  content: string;
}

// ---------- 愿望 ----------
export interface WishDto {
  id: number;
  wishType: number;
  title: string;
  description?: string;
  expectTime?: string;
  priority: number;
  status: number;
  claimUserId?: number;
  claimUserName?: string;
  completeTime?: string;
  completeRemark?: string;
  completeImage?: string;
  createUserId: number;
  createTime: string;
}
export interface WishReq {
  wishType: number;
  title: string;
  description?: string;
  expectTime?: string;
  priority: number;
  status: number;
}

// ---------- 待办 ----------
export interface TodoDto {
  id: number;
  title: string;
  description?: string;
  isDone: boolean;
  doneTime?: string | null;
  doneUserId?: number | null;
  doneUserName?: string | null;
  priority: number;
  dueTime?: string | null;
  category?: string | null;
  assigneeUserId?: number | null;
  assigneeName?: string | null;
  createUserId: number;
  createTime: string;
}
export interface TodoReq {
  title: string;
  description?: string;
  priority: number;
  dueTime?: string | null;
  category?: string;
  assigneeUserId?: number | null;
}

// ---------- 留言板 ----------
export interface BoardMessageDto {
  id: number;
  content: string;
  authorName?: string | null;
  color?: string | null;
  pinned: boolean;
  imageUrl?: string | null;
  receiverUserId?: number | null;
  isPrivate: boolean;
  scheduledAt?: string | null;
  isUnlocked: boolean;
  createUserId: number;
  createTime: string;
}
export interface BoardMessageReq {
  content: string;
  color?: string;
  imageUrl?: string;
  isPrivate?: boolean;
  receiverUserId?: number | null;
  scheduledAt?: string | null;
}

// ---------- 默契问答 ----------
export interface QuizQuestionDto {
  id: number;
  text: string;
  options: string[];
  category?: string | null;
  isBuiltin: boolean;
}
export interface QuizQuestionReq {
  text: string;
  options: string[];
  category?: string;
}
export interface QuizRoundDto {
  id: number;
  questionId: number;
  questionText: string;
  options: string[];
  category?: string | null;
  firstUserId?: number | null;
  /** 仅在 isRevealed 为 true 时后端才下发，未揭晓时为 null（防提前偷看） */
  firstAnswer?: number | null;
  firstAnsweredTime?: string | null;
  secondUserId?: number | null;
  secondAnswer?: number | null;
  secondAnsweredTime?: string | null;
  isRevealed: boolean;
  isMatched: boolean;
  /** 当前登录者是否已作答 */
  myAnswered: boolean;
  /** 当前登录者选的选项索引（自己的始终可见） */
  myAnswer?: number | null;
  /** 对方是否已作答 */
  mateAnswered: boolean;
  createUserId: number;
  createTime: string;
}
export interface QuizStatsDto {
  totalRounds: number;
  revealedRounds: number;
  matchedRounds: number;
  /** 默契率 0-100，按已揭晓局计算 */
  matchRate: number;
  pendingRounds: number;
}

// ---------- 相册 / 图片 ----------
export interface AlbumDto {
  id: number;
  albumName: string;
  cover?: string;
  remark?: string;
  imageCount: number;
  createUserId: number;
  createTime: string;
}
export interface AlbumReq {
  albumName: string;
  cover?: string;
  remark?: string;
}
export interface ImageDto {
  id: number;
  albumId: number;
  imagePath: string;
  url?: string;
  remark?: string;
  shootTime?: string;
  location?: string;
  createUserId: number;
  createTime: string;
}

// ---------- 矛盾 ----------
export interface ConflictDto {
  id: number;
  occurTime: string;
  summary: string;
  conflictLevel: number;
  myThoughtA?: string;
  myThoughtB?: string;
  reconcileTime?: string;
  reconcileWay?: string;
  reflectA?: string;
  reflectB?: string;
  ruleConclusion?: string;
  createUserId: number;
  createTime: string;
}
export interface ConflictReq {
  occurTime: string;
  summary: string;
  conflictLevel: number;
  myThoughtA?: string;
  myThoughtB?: string;
  reconcileTime?: string;
  reconcileWay?: string;
  reflectA?: string;
  reflectB?: string;
  ruleConclusion?: string;
}

// ---------- 记账 ----------
export interface AccountRecordDto {
  id: number;
  recordType: number;
  category: string;
  amount: number;
  recordTime: string;
  remark?: string;
  createUserId: number;
  createTime: string;
}
export interface AccountRecordReq {
  recordType: number;
  category: string;
  amount: number;
  recordTime: string;
  remark?: string;
}

// ---------- 批量导入账单 ----------
export interface AccountImportReq {
  csv: string;
}
export interface AccountImportRow {
  lineNo: number;
  valid: boolean;
  error?: string;
  recordType: number; // 1=收入 2=支出
  category: string;
  amount: number;
  recordTime: string;
  remark?: string;
}
export interface AccountImportError {
  lineNo: number;
  reason: string;
}
export interface AccountImportResult {
  total: number;
  imported: number;
  skipped: number;
  failed: number;
  errors: AccountImportError[];
}

// ---------- 预算 ----------
export interface BudgetDto {
  id: number;
  year: number;
  month: number;
  category?: string; // 空 = 当月总预算
  limitAmount: number;
}
export interface BudgetSetReq {
  year: number;
  month: number;
  category?: string;
  limitAmount: number;
}
export interface MonthlyCategoryStat {
  category: string;
  amount: number;
  budget?: number;
  isOverspent: boolean;
}
export interface MonthlyBudgetDto {
  year: number;
  month: number;
  income: number;
  expense: number;
  totalBudget?: number;
  remaining: number;
  isOverspent: boolean;
  categories: MonthlyCategoryStat[];
}
export interface AccountTrendDto {
  month: string; // "yyyy-MM"
  income: number;
  expense: number;
}
export interface AccountStatisticsDto {
  year: number;
  month: number;
  monthIncome: number;
  monthExpense: number;
  trend: AccountTrendDto[];
}

// ---------- 约会 ----------
export interface DateRecordDto {
  id: number;
  isCompleted: boolean;
  planTime?: string;
  realTime?: string;
  location?: string;
  budget?: number;
  realCost?: number;
  experienceScore?: number;
  remark?: string;
  createUserId: number;
  createTime: string;
}
export interface DateRecordReq {
  isCompleted: boolean;
  planTime?: string;
  realTime?: string;
  location?: string;
  budget?: number;
  realCost?: number;
  experienceScore?: number;
  remark?: string;
}

// ---------- 消息 ----------
export interface SystemMessageDto {
  id: number;
  receiverUserId: number;
  title: string;
  content?: string;
  messageType: number;
  isRead: boolean;
  createTime: string;
}

export interface TimelineItemDto {
  id: number;
  type: string;
  title: string;
  date: string;
  summary?: string;
  relatedId: number;
  isYearly?: boolean;
  nextOccurrence?: string | null;
}

// ---------- 绑定结果（重签令牌随响应返回） ----------
export interface JoinResult {
  partner: PartnerInfo;
  tokens: LoginResp;
}

// ---------- 用户 / 导出 ----------
export interface UpdateProfileReq {
  nickName?: string;
  avatar?: string;
  oldPassword?: string;
  newPassword?: string;
}
// 导出改为「一次性下载令牌」：服务器映射临时目录中的 zip，带短 TTL 且下载即作废，
// 绝不返回公开可猜 URL，规避无鉴权可下载导致的 PII 泄露。
export interface ExportResp {
  token: string;
  fileName: string;
  mediaCount?: number;
}

// ---------- 足迹 / 自定义计数卡 ----------
export interface FootprintDto {
  id: number;
  title: string;
  emoji: string;
  count: number;
  lastIncrementTime?: string;
  targetCount?: number | null;
  description?: string | null;
  createUserId: number;
  createTime: string;
}
export interface FootprintReq {
  title: string;
  emoji: string;
  targetCount?: number | null;
  description?: string | null;
}

// ---------- 每日一句温情语录 ----------
export interface DailyQuoteDto {
  content: string;
  author?: string;
}
