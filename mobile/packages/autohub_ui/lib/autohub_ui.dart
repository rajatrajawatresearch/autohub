/// The Registry design language, as Flutter widgets.
///
/// The only package in this workspace that knows what a colour is. It knows nothing about
/// vehicles: there is no fuel type here, no listing status, no owner-count vocabulary, no fee
/// percentage. Every word a user reads arrives as a parameter from the caller, resolved from the
/// localisation registry, and every status tone is derived from a workflow *category* rather than
/// chosen from a state name.
///
/// Start from `canon/plate_block.dart`. It is the signature of the product and the reference for
/// the style, the comment density and the API shape of everything else in here.
library;

export 'src/canon/ah_badge.dart';
export 'src/canon/ah_button.dart';
export 'src/canon/ah_skeleton.dart';
export 'src/canon/ah_text_field.dart';
export 'src/canon/bid_ticket.dart';
export 'src/canon/empty_state.dart';
export 'src/canon/field_block.dart';
export 'src/canon/money_value.dart';
export 'src/canon/odometer_value.dart';
export 'src/canon/plate_block.dart';
export 'src/canon/plate_class_wire.dart';
export 'src/canon/plate_input.dart';
export 'src/canon/section_heading.dart';
export 'src/canon/spec_strip.dart';
export 'src/canon/status_pill.dart';
export 'src/canon/theme_toggle.dart';
export 'src/canon/vehicle_card.dart';
export 'src/canon/workflow_timeline.dart';
export 'src/theme/ah_density.dart';
export 'src/theme/ah_theme.dart';
export 'src/tokens/colors.dart';
export 'src/tokens/typography.dart';
