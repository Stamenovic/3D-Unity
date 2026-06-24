# Sci-Fi Space UI Design Notes

## Ideja

Projekat je o kontroleru karaktera i sinhronizaciji animacija kretanja u 3D prostoru. UI zato ne treba da izgleda kao obican meni, nego kao sistemski HUD iz svemirskog odela ili brodskog simulatora. Igrac treba da ima osecaj da testira EVA/space suit mobilnost u sci-fi stanici.

## Vizuelni pravac

- Tema: sci-fi space station / EVA suit interface.
- Primarne boje: tamni grafit i skoro crna pozadina za "space glass" panele.
- Akcenti: cyan za aktivne sisteme, amber za upozorenja i mission objective.
- Stil: tanki uglovi, svetleci tekst, prozirni paneli, reticle u centru ekrana.
- Bez velikih dekorativnih blokova: UI treba da izgleda kao funkcionalan sistem, ne kao web stranica.

## HUD elementi

- Start screen: `ORBITAL TRAINING`
  - Prvi ekran koji igrac vidi kad se pokrene Play Mode.
  - Gameplay je pauziran dok igrac ne klikne `STARTUJ MISIJU`.
  - Dugme `PODESAVANJA` otvara audio i visual FX podesavanja.
  - Dugme `EXIT` u Unity editoru izlazi iz Play Mode-a.

- Top-left panel: `EVA SUIT HUD // MOBILITY`
  - Prikazuje brzinu karaktera u m/s.
  - Ima speed bar koji se puni prema trenutnoj brzini.
  - Stanje se menja izmedju `IDLE STANDBY`, `MOTION ACTIVE`, `BOOST RUN` i `AIRBORNE VECTOR`.
  - Grounded status je prikazan kao `GROUND LOCK` ili `GROUND LOST`.

- Top-right panel: `MISSION OBJECTIVE`
  - Daje projektnu svrhu: kalibracija kretanja i animacija.
  - Podseca da treba testirati walk, sprint, jump i animacijski odziv.
  - Sadrzi `ESC // SYSTEM MENU` hint.

- Center reticle:
  - Mali crosshair daje osecaj kacige/visor-a.
  - Ne pokriva igraca i ne smeta gameplay-u.

- Bottom-left control strip:
  - Kratke kontrole: `WASD`, `SHIFT`, `SPACE`, `MOUSE`.
  - Postavljen dole da ne zaklanja bitne delove scene.

## Pause menu

- Naziv: `COMMAND DECK`
- Stanje: `SIMULATION PAUSED`
- Akcije:
  - `RESUME MISSION`
  - `PODESAVANJA`
  - `RESTART SECTOR`
  - `EXIT SIMULATION`

## Podesavanja

- `BACKGROUND MUZIKA`
  - Ukljucuje ili iskljucuje proceduralnu sci-fi ambient muziku.
  - Slider kontrolise jacinu muzike.
- `SPECIJALNI EFEKTI`
  - Ukljucuje ili iskljucuje dodatne HUD efekte kao sto su reticle i scanline overlay.
  - Slider kontrolise intenzitet efekata.
- Visual style:
  - Veliki centralni hologramski panel kao brodski terminal.
  - Naslov `PODEŠAVANJA` je u zasebnoj gornjoj plocici.
  - Sekcija `ZVUK` deli panel horizontalnom cyan linijom.
  - Levo su velike ikonice za muziku i efekte, desno slideri i checkbox kontrole.
  - `[POVRATAK]` je dole desno, kao sistemska komanda.

## User flow

1. Igrac klikne Play u Unity editoru.
2. Prikazuje se `ORBITAL TRAINING` start meni preko postojece sci-fi scene.
3. `STARTUJ MISIJU` zakljucava kursor, pokrece vreme i aktivira player/camera kontrole.
4. `ESC` tokom igre otvara `COMMAND DECK` pause meni.
5. `EXIT` izlazi iz Play Mode-a u editoru.

Ovo zvuci vise kao sci-fi simulator nego kao genericki meni. Time projekat dobija jedinstven identitet i bolje se uklapa uz scifi corridor assete.

## Sledeci design koraci

1. Dodati animation state panel kad animator bude spreman.
2. Dodati male status ikone za walk/run/jump/crouch/slide.
3. Dodati kratki intro overlay: `Suit systems online`.
4. Dodati sound feedback za hover/click i pause.
5. Ako ostane vremena, napraviti UI prefab umesto runtime-only generisanja.
