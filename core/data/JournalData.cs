namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-data store for Aoife's journal entries.
/// Lives in core/data so it can be unit-tested without the Godot runtime.
/// </summary>
public static class JournalData
{
    public readonly record struct JournalEntry(string Date, string Title, string Body);

    public static readonly JournalEntry[] Entries =
    [
        new(
            Date:  "10/3/1160",
            Title: "First entry",
            Body:
@"Linnie keep on bugin me to start makin a jornal so here. My naem is Aoife Sylzair and I m sevn years old. I live in the Trid Trindis Trindilliastica right up near the snowi mounins. I have a twin sister named Ulalynn and we are best freinds even though we fihgt sometimes. It is fall rihgt now and I love the Rakl potato melts and hot cider and the leafs. My favrite colour is dark yellow NOT reglar yellow like a banana. When I grow up, I watn to be a doctor and heal pepople. Thank you for your time.

- Effy"
        ),

        new(
            Date:  "5/10/1193",
            Title: "Arriving in Argyre",
            Body:
@"As our ship neared the port, I heard a cacophony of several different bells ringing in different pitches, booming in my chest. Ahead of us was a channel leading to an enormous, beautiful brick bridge. The interlaid bricks formed a spiraling pattern that was almost hypnotic. So hypnotic, in fact, that I noticed too slowly that the ship was on a crash-course towards the bridge. As I yelled out to the crew about the impending doom, I received stifled laughter as the boat suddenly began to… sink? As the ship sank downwards, I could feel the pull of a strong current propelling us forward. As the water became level with the deck, I closed my eyes and held my breath anticipating the flooding water.

After the initial anticipation wore off, I realized that it had been a few seconds since the impending flood. As I opened my eyes, I saw fish floating within arms-reach of the ship, but… above! I could clearly see now that the boat had been surrounded by some sort of air bubble surrounded by the sea at all angles. Gradually, we came back up to the surface as we pulled into a pristine, sparkling bay. The crew laughed and apologetically explained that it is tradition in Argyre to not tell newcomers as a surprise. Assholes.

- The very pissed off Aoife"
        ),

        new(
            Date:  "5/15/1193",
            Title: "You've probably guessed who at this point",
            Body:
@"I have been getting used to the sights of Argyre and it is honestly beautiful. They put me up in the Tulis Lighthouse Lodge and gods is it the fanciest place I have ever stayed. I have also stopped at a bakery called Two Fat Cats way too often, I think I have a bit of a pudge going. Anyways, guess who I met today? Rork! He seems like he's barely aged a day… How old is he, anyways? Anyways again, we caught up and I treated him to a meal at Danny's. He's about to go on an expedition to the Izotz Channel, which is far to the northeast. Apparently no one has actually gone there and lived to tell the tale. I'm sure he'll be alright though, I have literally seen him take a blunderbuss to the face.

- The old lady you see on a park bench sitting looking at the waves, Aoife"
        ),

        new(
            Date:  "3/8/1207",
            Title: "The Gravity of the situation",
            Body:
@"Well, Radovast has broken his arm. You think someone would have warned us that this valley had fucked up gravity in it and standing in the wrong spot can change your orientation. He's lucky he wasn't sent into the stratosphere.

- Aoife"
        ),

        new(
            Date:  "07/08/1210",
            Title: "The Lake and the River",
            Body:
@"The day has started out great, we found perhaps the most beautiful lake I have ever seen. The calm here is incredible, Ulalynn, even quieter than our cozy little town. Rime Saydlis, the ice Genasi I told you about, has been talking about this the entire time. He has been researching the ancestral home of water elementals, Genasi, and Gyrefolk and believes he has finally found it. We are going to head down into the ruins now…

Fucking hell, Ula. It happened again. Rime solved a puzzle left behind by whoever made this place, and saw a carving on the wall. He looked so happy. I noticed the trap too late, and then… he was gone. We couldn't even give him a proper burial. After that, we found exactly what he was looking for; one of the most serene places I have ever seen in my life. We stayed there for hours in complete silence. I'll have to tell you all about it when I'm back.

After we got out of the ruins and made camp, the party discussed with each other and decided to call this Saydlis Lake in his memory. I think he'd like that.

- Your loving sister and idiot explorer, Aoife"
        ),

        new(
            Date:  "07/25/1210",
            Title: "The Volcano",
            Body:
@"What in God's name are these fucked-up lava creatures? There's no way we're going near that shit, we're going around.

- Aoife"
        ),

        new(
            Date:  "12/5/1210",
            Title: "Final Entry",
            Body:
@"By the time I write this, I will be dead. No, but seriously, how does anyone write something like this shit? I have tried to write this so many times, but it simply cannot be read, even by me. After being saved, I met Lilei who is a follower of Mystra. She claims to want to enchant this journal to keep them away from prying eyes, but the rest of this journal other than this page looks exactly the same to me. However, Amethyst Dragon can be quite tricky from what I have read so I'm sure there will be some sort of trick to this. I'll be sending back the journal                              but this is where the MAPP expedition ends for me. To put it plainly:

DO NOT SEND ANYONE ON ANY MORE EXPEDITIONS, THEY WILL ONLY DIE.

THE DANGER LIES IN THE NORTH.

- Aoife Sylzair"
        ),
    ];
}
