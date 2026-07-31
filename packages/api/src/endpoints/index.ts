/**
 * Typed functions, one per endpoint. These are the seam: everything above them speaks TypeScript,
 * everything below them speaks HTTP.
 */

export * as platform from './platform';
export * as auth from './auth';
export * as catalog from './catalog';
export * as valuation from './valuation';
export * as listings from './listings';
export * as auction from './auction';
export * as deals from './deals';

/* Named re-exports for the calls apps reach for constantly. */
export {
  getTaxonomyTree,
  getAttributes,
  getMakes,
  getModels,
  getVariants,
  getFormDefinition,
  validateForm,
  getWorkflowDefinition,
  getWorkflowSnapshot,
  fireWorkflowAction,
  getPricingQuote,
  getFeatures,
  getI18nBundle,
  getCities,
} from './platform';
export { requestOtp, verifyOtp, getMe, logout } from './auth';
export { search, toSearchQuery } from './catalog';
export { createValuationQuote } from './valuation';
export { listListings, getListing, createListingDraft, updateListing } from './listings';
export type { ListingPage } from './listings';
export {
  listAuctionLots,
  getAuctionLot,
  getLotBids,
  placeBid,
  openAuctionStream,
} from './auction';
export type { AuctionLotPage, BidPage, AuctionStreamOptions, AuctionStreamStatus } from './auction';
export { listDeals, getDeal, makeOffer } from './deals';
export type { DealPage } from './deals';
