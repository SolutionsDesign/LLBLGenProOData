## OData Support Classes library for [LLBLGen Pro](https://www.llblgen.com)

This is the repository for the OData Support Classes library for [LLBLGen Pro](https://www.llblgen.com) v5.x. It contains the sourcecode for the SD.LLBLGen.Pro.ODataSupportClasses library of v5.6.x so users who need to support OData can continue to do so. Starting with v5.7, [LLBLGen Pro](https://www.llblgen.com) doesn't officially support OData anymore, and as a courtesy to our existing customers we have published the sourcecode for the OData Support Classes here on GitHub under the flexible MIT license.

The code in this repository references LLBLGen Pro v5.6 assemblies. It's easy to adjust them to the LLBLGen Pro version you're using though. 

### Compatiblity
The sourcecode available here compiles against [LLBLGen Pro](https://www.llblgen.com) v5.6.x assemblies. If you encounter code breaking changes, please file an issue on this repository so we can look into this. 

### Support on this sourcecode
While [LLBLGen Pro](https://www.llblgen.com) officially doesn't support OData anymore, we strive to keep this code compileable against the latest [LLBLGen Pro](https://www.llblgen.com) version, if feasible. Please file an issue here if you run into an issue with compiling the code. 

The documentation for OData support in the LLBLGen Pro Runtime Framework can be found here: [official LLBLGen Pro v5.6 documentation](https://www.llblgen.com/Documentation/5.6/LLBLGen%20Pro%20RTF/Using%20the%20generated%20code/gencode_wcfdataservices.htm). 

This codebase supports OData services v1, v2 and v3. If you need v4 you have to refactor this library to meet its requirements. Microsoft added breaking changes in their v6 WCF Data Services system hence we discontinue our support for it as it would mean a second codebase to support. 

### License
The sourcecode in this repository is licensed to you under the MIT license, given below.

```
The MIT License (MIT)

Copyright (c) 2020 Solutions Design bv

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

#### NuGet
You are not allowed to publish the assemblies, compiled from the code in this repository, on NuGet as separate packages. 
