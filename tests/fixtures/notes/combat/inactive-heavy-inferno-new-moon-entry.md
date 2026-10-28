# Inactive heavy Inferno new moon entry

Random campaign 25's original Ring 8 reward deck produced an unsupported
EndTurn after 47 inputs. Two original `HeavyT3_PyreDamageIfUnblocked` enemies
entered the original Inferno room while moon tracking was inactive and the
phase was new moon. Each native mask changed from 6 to 5. The EndTurn defeated
the player; it is not a winning battle or a default-search acceptance.

The original 0.2.391 observation and an independent 0.2.392 model replay
match every input, comparable native successor and gameplay RNG. The focused
checker replays the complete chain, compares terminal battle state and RNG,
and excludes terminal card-play rules under the existing terminal convention.
Four concurrent model branches preserve the parent. Separate identity, attack,
health and trigger-count perturbations exercise the isolated room-entry guard;
unknown enemy and room payloads remain unsupported. The existing signature
still requires the original enemy trigger and original Inferno payload.
