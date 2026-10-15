# Atomic shop purchase verification

This repository is a code extract. Changes were first implemented and tested in
the full Unity 6000.3.2f1 project with Firebase Unity SDK 13.6.0.

**12 opt-in Firestore emulator integration tests passed** using demo-only data.
Coverage includes fixed/random/map purchases, invalid price and insufficient gold,
double clicks, server-side duplicate ownership, two independent client instances,
denied writes, a physically blocked loopback connection, stale profile saves,
server-side reloads, and the persistent purchase state observed by a reopened shop.

The public DatabaseManager, ShopManager, ShopPopupUI, ShopUIController, and the public
UserGameData model also passed a C# compile with the full project's real Unity and
Firebase references. Unity Inspector-assigned fields produce the usual CS0649
warnings. This is not a full Unity build/runtime test of the public extract.

Gold, purchased ownership, and a character's linked icon commit in one Firestore
transaction. Local application and callback-based purchase success follow commit.
Transaction retries never mutate live local data. Failure/cancellation release
the purchase gate without a successful callback. Closed/destroyed/reopened views
ignore stale presentation and observe the persistent purchase state.

Existing profile saves omit authoritative gold and union unlock IDs. Gold reward
writes share the local economy gate and use server balances so other save paths
cannot undo purchases. Default icon and owned-item selection paths remain intact.

The public snapshot's older UserGameData shape and nonlocalized UI text are
preserved. Private ranking/localization changes and Firebase configuration were
not copied into this repository.

Still required: actual mobile UI/sound/confetti/lock states, close/reopen and scene
destruction during purchase, physical mobile disconnect/reconnect, app restart,
and staging checks with real security rules. Older deployed clients that still
replace whole user documents can overwrite newer writes; coordinate rollout or
enforce economy invariants on the backend. No production database was accessed.
