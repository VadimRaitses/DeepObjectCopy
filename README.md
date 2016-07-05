"# DeepObjectCopy" 
Deep copy is algorithm to copy c# objects from one namespace to another
very useful for work with different references to same server endpoints(dmz to lan for example) when proxy reference is existing in another dll (BL)

for example MayNamespace1.FooRequest to MyNamespace.ProxyClass.FooRequest
or 
 MayNamespace1.FooResponse to MyNamespace.ProxyClass.FooResponse
