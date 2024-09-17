# ICU Shim

ICU Shim is a surrogate for `icu.dll`, a system DLL introduced in Windows 10 1903.
`icu.dll` is not available on older Windows 10 versions and Windows Server 2019.

The ICU shim DLL exports a subset of the functions exported by `icu.dll`, just
enough to satisfy the imports of `Microsoft.Terminal.Control.dll`. The DLL
doesn't implement any actual logic, instead all functions fail with `U_UNSUPPORTED_ERROR`.