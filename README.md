# AR LogicLab

AR LogicLab is a Unity 6.3 LTS learning app for experimenting with Boolean logic gates on Android. The learning screens work without a camera or AR device; the Circuit Lab can also place a 3D circuit on a detected real surface.

## Learning experience

- Five short lessons introduce binary inputs, AND/OR, inverted gates, exclusive logic, and circuit verification.
- The interactive lab covers AND, OR, NOT, NAND, NOR, XOR, and XNOR. Switches update the truth-table output, matching code, and placed AR circuit together.
- The 28-question quiz contains each of the four binary input combinations for all seven gates. Correct answers are explained and scores persist on the device.
- The screen fits the phone safe area, uses large touch controls, keeps lesson text visible as captions, and offers Android text-to-speech plus optional sound cues.

## Open and build

1. Open this folder in Unity **6000.3 LTS**. Let Package Manager finish resolving packages.
2. Open **Assets/Scenes/ARLearning.unity**. The scene is already the first enabled Android build scene.
3. In **File → Build Profiles**, choose Android. Use **Build** to create an APK without a connected phone. Use **Build And Run** only after the phone appears in **Run Device** and USB debugging is authorized. Allow camera access on the phone.
4. Open **Circuit Lab**, choose a gate, move the phone slowly until a plane is found, and tap the camera view to place a circuit. If a surface is not available, the on-screen inputs and truth-table simulation still work.

The project uses AR Foundation and ARCore 6.3.5 with the Input System. Unity's editor may show “No active XRPlaneSubsystem” while not running on an AR-capable device; plane detection is supplied by the device runtime. A headset or physical phone is needed to verify camera tracking and placement.

## Checks

Edit Mode tests in `Assets/Tests/EditMode` cover representative truth-table rows, the seven-gate/28-case quiz bank, and the five-lesson catalog. Run them from **Window → General → Test Runner → EditMode**.

## Android device and console messages

- `No Android devices connected` appears when **Build And Run** cannot see a phone through ADB. Use **Build** to make the APK; install it on the phone separately.
- Enable USB debugging in Developer options, use **File transfer** mode, reconnect, and accept the phone's **Allow USB debugging** prompt. If ADB still sees no device, check the USB cable, port, and driver.
- `1 URP assets included in build` is informational.
- If Unity reports that Diagnostics Data needs symbols, choose **Public (Symbol Table)** in Android **Debug Symbols**. Unity creates a `.symbols.zip` archive for crash stack traces.
