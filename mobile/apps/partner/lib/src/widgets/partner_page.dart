import 'package:autohub_ui/autohub_ui.dart';
import 'package:flutter/material.dart';

/// A tab root: a heading, one optional action, and a scrolling body.
///
/// The heading carries the same uppercase letter-spaced eyebrow that sits above every value in the
/// product. That one repetition is what makes a vehicle list, a match board and a settlement
/// statement read as pages of one document rather than three screens by three people.
class PartnerBoard extends StatelessWidget {
  const PartnerBoard({
    required this.title,
    required this.children,
    this.eyebrow,
    this.description,
    this.action,
    this.onRefresh,
    super.key,
  });

  final String title;
  final String? eyebrow;
  final String? description;

  /// One control. Two means the board is doing two jobs, and the fix is two boards.
  final Widget? action;

  /// Pull-to-refresh. Present on every board that reads from the network, because the alternative
  /// on a phone is closing and reopening the app — which is what partners actually do, and it
  /// costs them a cold launch.
  final Future<void> Function()? onRefresh;

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final Widget body = ListView(
      padding: const EdgeInsets.fromLTRB(AhSpace.lg, AhSpace.lg, AhSpace.lg, AhSpace.xxxl),
      children: <Widget>[
        SectionHeading(
          eyebrow: eyebrow,
          title: title,
          description: description,
          action: action,
          rule: true,
        ),
        const SizedBox(height: AhSpace.lg),
        ...children,
      ],
    );

    final Future<void> Function()? refresh = onRefresh;
    if (refresh == null) return body;

    return RefreshIndicator(
      onRefresh: refresh,
      color: context.ahSurfaces.accentPrimary,
      child: body,
    );
  }
}

/// A pushed page: an app bar with a back affordance, and a scrolling body.
class PartnerDetailPage extends StatelessWidget {
  const PartnerDetailPage({
    required this.title,
    required this.children,
    this.scrollable = true,
    super.key,
  });

  final String title;

  /// Set `false` where the body owns its own scrolling — a `SchemaForm` inside a `ListView` would
  /// otherwise nest two scroll views and the outer one would swallow the inner one's drags.
  final bool scrollable;

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    return Scaffold(
      appBar: AppBar(
        backgroundColor: surfaces.card,
        surfaceTintColor: surfaces.card,
        elevation: 0,
        scrolledUnderElevation: 0,
        // Structure from a hairline, as everywhere else. An elevated bar would be the only shadow
        // in the product.
        shape: Border(bottom: BorderSide(color: surfaces.borderHairline)),
        title: Text(title, style: AhText.subheading.copyWith(color: surfaces.textPrimary)),
      ),
      body: SafeArea(
        top: false,
        child: scrollable
            ? ListView(
                padding: const EdgeInsets.fromLTRB(
                  AhSpace.lg,
                  AhSpace.lg,
                  AhSpace.lg,
                  AhSpace.xxxl,
                ),
                children: children,
              )
            : Padding(
                padding: const EdgeInsets.all(AhSpace.lg),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: children,
                ),
              ),
      ),
    );
  }
}

/// A hairline-bordered card. The one container shape in this app.
class PartnerCard extends StatelessWidget {
  const PartnerCard({required this.child, this.onTap, super.key});

  final Widget child;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final AhSurfaces surfaces = context.ahSurfaces;

    final Widget painted = DecoratedBox(
      decoration: BoxDecoration(
        color: surfaces.card,
        borderRadius: const BorderRadius.all(AhRadius.card),
        border: Border.all(color: surfaces.borderHairline),
      ),
      child: Padding(padding: const EdgeInsets.all(AhSpace.md), child: child),
    );

    if (onTap == null) return painted;

    return Material(
      type: MaterialType.transparency,
      child: InkWell(
        onTap: onTap,
        splashFactory: NoSplash.splashFactory,
        hoverColor: Colors.transparent,
        focusColor: Colors.transparent,
        highlightColor: Colors.transparent,
        child: painted,
      ),
    );
  }
}
