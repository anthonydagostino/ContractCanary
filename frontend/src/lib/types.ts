export type PlanTier = 'None' | 'Starter' | 'Pro'
export type SubscriptionStatus =
  | 'None' | 'Trialing' | 'Active' | 'PastDue' | 'Canceled'
  | 'Incomplete' | 'IncompleteExpired' | 'Unpaid' | 'Paused'

export type NoticeType =
  | 'Unknown' | 'Presolicitation' | 'Solicitation' | 'CombinedSynopsis'
  | 'SourcesSought' | 'SpecialNotice' | 'AwardNotice' | 'Justification'
  | 'SaleOfSurplus' | 'IntentToBundle'

export type SetAsideCode =
  | 'None' | 'TotalSmallBusiness' | 'PartialSmallBusiness' | 'EightA' | 'EightASoleSource'
  | 'HubZone' | 'HubZoneSoleSource' | 'Sdvosb' | 'SdvosbSoleSource' | 'Wosb' | 'WosbSoleSource'
  | 'Edwosb' | 'EdwosbSoleSource' | 'LocalArea' | 'IndianEconomicEnterprise'
  | 'IndianSmallBusinessEE' | 'BuyIndian' | 'VeteranOwned' | 'Other'

export interface PlanLimits {
  maxProfiles: number
  canExportCsv: boolean
  canPrioritize: boolean
  dailyDigest: boolean
}

export interface Me {
  id: string
  email: string
  emailConfirmed: boolean
  fullName?: string
  companyName?: string
  timeZoneId: string
  isAdmin: boolean
  plan: PlanTier
  limits: PlanLimits
  subscriptionStatus: SubscriptionStatus
  trialEndsAt?: string
  currentPeriodEndsAt?: string
  cancelAtPeriodEnd: boolean
}

export interface AuthTokens {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

export interface MatchProfile {
  id: string
  name: string
  naics: string[]
  psc: string[]
  keywords: string[]
  agencyPaths: string[]
  setAsides: SetAsideCode[]
  states: string[]
  noticeTypes: NoticeType[]
  isActive: boolean
  isPriority: boolean
  matchCount: number
  createdAt: string
  updatedAt: string
}

export type ProfileInput = Omit<MatchProfile, 'id' | 'matchCount' | 'createdAt' | 'updatedAt'>

export interface NoticeListItem {
  noticeId: string
  title: string
  solicitationNumber?: string
  type: NoticeType
  typeLabel: string
  agencyPath?: string
  departmentName?: string
  naicsCode?: string
  pscCode?: string
  setAside: SetAsideCode
  setAsideLabel?: string
  postedDate: string
  responseDeadline?: string
  popState?: string
  popCity?: string
  uiLink?: string
  isSaved: boolean
  isMatched: boolean
}

export interface MatchedProfile {
  profileId: string
  profileName: string
  matchReason?: string
}

export interface NoticeDetail extends NoticeListItem {
  baseType?: string
  subTierName?: string
  officeName?: string
  setAsideDescription?: string
  archiveDate?: string
  popZip?: string
  popCountry?: string
  description?: string
  descriptionLink?: string
  primaryContactName?: string
  primaryContactEmail?: string
  primaryContactPhone?: string
  isActive: boolean
  matchedProfiles: MatchedProfile[]
  savedNote?: string
  aiSummary?: string
  aiKeyPoints: string[]
  aiFitNote?: string
  aiModel?: string
  aiGeneratedAt?: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface NaicsDto { code: string; title: string; level: number }
export interface PscDto { code: string; title: string; category: string; isService: boolean }
export interface AgencyDto { code: string; name: string; tier: number; parentCode?: string }
export interface SetAsideDto { code: string; name: string; description: string }
export interface NoticeTypeDto { value: NoticeType; label: string }

export interface PlanCatalogItem {
  tier: PlanTier
  name: string
  monthlyPriceUsd: number
  blurb: string
  features: string[]
}

export interface Meta {
  product: { productName: string; tagline: string; supportEmail: string; companyLegalName: string }
  trialDays: number
  plans: PlanCatalogItem[]
}

export interface AdminMetrics {
  totalUsers: number
  adminUsers: number
  activeSubscribers: number
  trialing: number
  starterSubscribers: number
  proSubscribers: number
  pastDue: number
  canceled: number
  totalNotices: number
  activeNotices: number
  noticesLast24h: number
  totalMatches: number
  emailsSentTotal: number
  emailsSentLast24h: number
  emailFailuresLast24h: number
  lastIngestRun?: {
    source: string
    status: string
    startedAt: string
    completedAt?: string
    noticesInserted: number
    noticesUpdated: number
    matchesCreated: number
    error?: string
  }
}

export interface NoticeQueryParams {
  search?: string
  naics?: string[]
  psc?: string[]
  setAsides?: SetAsideCode[]
  states?: string[]
  noticeTypes?: NoticeType[]
  agency?: string
  matchedOnly?: boolean
  profileId?: string
  savedOnly?: boolean
  sort?: 'posted' | 'deadline'
  direction?: 'asc' | 'desc'
  page?: number
  pageSize?: number
}
