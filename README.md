#The problem – Tons of repetitive code segments
Usually we found ourselves writing very similar codes for defining the state of entities.  Normally, the procedure is as follows:

-	Determine which properties are needed for defining existence of the entity in the database (this could be primary key properties or unique key properties).
-	If the result is null then the entity must be inserted.
-	If the result is not null and if a change has occurred in any part of the entity, then the entity must be updated.
-	If we have a collection of entities, then we need to compare it with the ones in the database and delete those which are not exist in the collection anymore.

and so on …

###Additional explanation about why sometimes we need unique key properties in addition to the primary key properties

Say we have Phone entity which has some properties: 
     ID,
     Digits, 
     Prefix
     …

ID is auto-generated primary key. In the meanwhile, we do not want to insert same phone number to the table with a different ID. So Digits and Prefix properties are unique together. This situation is forcing us to take into consideration this:

If Id is 0, and there is not any corresponding entity in the database with the specified Digits and prefix, then it must be inserted. Otherwise, if a change has occurred, then it must be updated and so on…

Now, let’s do the same things all over again for a different entity graph. Again, and again… 

#The solution – Use EntityGraphOperations for Entity Framework Code First

###Features:
-	Automatically define state of all entities 
-	Update only those entities which has changed
-	Fluent API style mapping of special entity configurations
-	Let the user manually manage graph after automatically determining state of all entities

The example:
Let’s say I have get a Person object. Person could has many phones, a Document and could has a spouse.
    
```
public class Person
{
     public int Id { get; set; }
     public string FirstName { get; set; }
     public string LastName { get; set; }
     public string MiddleName { get; set; }
     public int Age { get; set; }
     public int DocumentId {get; set;}
   
     public virtual ICollection<Phone> Phones { get; set; }
     public virtual Document Document { get; set; }
     public virtual PersonSpouse PersonSpouse { get; set; }
}
  ```

I want to determine the state of all entities which is included in the graph. 
```
context.InsertOrUpdateGraph(person)
       .After(entity =>
       {
            // Delete missing phones.
            entity.HasCollection(p => p.Phones)
               .DeleteMissingEntities();
               
            // Delete if spouse is not exist anymore.
            entity.HasNavigationalProperty(m => m.PersonSpouse)
                  .DeleteIfNull();
       });
```
 Also as you remember  unique key properties could play role while defining the state of Phone entity. For such special purposes we have `ExtendedEntityTypeConfiguration<>` class, which inherits from `EntityTypeConfiguration<>`. If we want to use such special configurations then we must inherit our mapping classes from `ExtendedEntityTypeConfiguration<>`, rather than `EntityTypeConfiguration<>`. For example:

```
public class PhoneMap: ExtendedEntityTypeConfiguration<Phone>
    {
        public PhoneMap()
        {
             // Primary Key
             this.HasKey(m => m.Id);
              …
             // Unique keys
             this.HasUniqueKey(m => new { m.Prefix, m.Digits });
        }
    }
```
That’s all.
