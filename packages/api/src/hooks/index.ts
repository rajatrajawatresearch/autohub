/**
 * TanStack Query hooks. Client components only — a Server Component reads through `@autohub/api/server`.
 */

export { queryKeys } from './keys';

export {
  useTaxonomyTree,
  useAttributes,
  useMakes,
  useModels,
  useVariants,
  useFeatures,
  useCities,
  useI18n,
} from './use-registry';

export { useFormDefinition, useValidateForm } from './use-forms';
export { useWorkflow, useWorkflowDefinition, useFireWorkflowAction } from './use-workflow';
export { usePricingQuote, useQuotePricing } from './use-pricing';
export { useSearch } from './use-search';
export type { UseSearchResult } from './use-search';
export { useValuationQuote, primaryValuation } from './use-valuation';
export { useMe, useRequestOtp, useVerifyOtp, useLogout } from './use-auth';
export { useListings, useListing, useCreateListingDraft, useUpdateListing } from './use-listings';
export {
  useAuctionLots,
  useAuctionLot,
  useLotBids,
  usePlaceBid,
  useAuctionStream,
} from './use-auction';
export type { UseAuctionStreamResult } from './use-auction';
export { useDeals, useDeal, useMakeOffer } from './use-deals';
