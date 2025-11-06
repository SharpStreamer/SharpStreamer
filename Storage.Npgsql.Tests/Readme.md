XUnit testing library works like that.
* Tests within the same test class:
  * Tests in the same class run sequentially (one after another).
  * This is intentional because: The same fixture (IClassFixture) instance is shared by all tests in that class (single fixture instance is shared for single test class). 
  * Running them concurrently could cause race conditions if they share state.

* Tests in different classes:
  * Tests in different classes run in parallel — as long as they are not part of the same collection (more on that below).
  * ```
    MyTests1
       ├── TestA  <-- sequential
       ├── TestB
    MyTests2
       ├── TestC  <-- runs in parallel with MyTests1’s tests, but sequentially with MyTests2's tests.
       ├── TestD```
