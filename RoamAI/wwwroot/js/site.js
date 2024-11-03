$(function () {
    

    const messages = [
        "Yükleniyor...",
        "Bunu Biliyor musunuz? Roma yılda 10,1 milyon turist ağırlar.",
        "Paris'in Eyfel Kulesi 12 yılda yapılmıştır.",
        "Yükleniyor...",
        "Venedik - Şehirdeki gondolların %99’u siyahtır; bu, geleneksel olarak yasla ilişkilendirilen bir renktir.",
        "Dubai - Burj Khalifa'nın zirvesine ulaşmak için 2.909 basamak tırmanmanız gerekir!",
        "Yükleniyor...",
        "Londra - Big Ben, aslında çanın adı; saatin resmi adı Elizabeth Kulesi’dir!",
        "Tokyo - Tokyo Kulesi, Eyfel Kulesi’nden 13 metre daha uzundur!",
        "Machu Picchu, deniz seviyesinden 2.430 metre yüksekliktedir.",
        "Antarktika, dünyanın en soğuk yeri olarak bilinir.",
        "Norveç, Kuzey Işıkları'nın en iyi gözlemlendiği yerlerden biridir.",
        "Hindistan'daki Himalayalar, dünyanın en yüksek zirvelerine ev sahipliği yapar.",
        "Avustralya'daki Büyük Set Resifi, dünyadaki en büyük mercan resifidir."
    ];

    let currentMessageIndex = 0;
    let messageInterval;

    function DisplayBusyIndication() {
       
        $('.loading').show();
        $('.loading h1').text(messages[currentMessageIndex]);

        // Clear any existing interval to prevent multiple intervals from running
        clearInterval(messageInterval);

        // Update the message every 5 seconds
        messageInterval = setInterval(function () {
            currentMessageIndex = (currentMessageIndex + 1) % messages.length;
            $('.loading h1').text(messages[currentMessageIndex]);
        }, 5000);
    }

    function HideBusyIndication() {
       
        $('.loading').hide();
        clearInterval(messageInterval); // Stop message rotation when hiding
        currentMessageIndex = 0; // Reset to the first message
    }

    $(window).on('beforeunload', function () {
       
        DisplayBusyIndication();
    });

    $(document).on('submit', 'form', function () {
        
        DisplayBusyIndication();
    });

    window.setTimeout(function () {
        HideBusyIndication();
    }, 2000);

    $(document).ready(function () {
        DisplayBusyIndication();
        console.log("DisplayBusyIndication called on page load");
    });
});